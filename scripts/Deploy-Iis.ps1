[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $StorageAccount,

    [Parameter(Mandatory = $true)]
    [string] $ContainerName,

    [Parameter(Mandatory = $true)]
    [string] $BlobName,

    [Parameter(Mandatory = $true)]
    [string] $PackageSha256,

    [Parameter(Mandatory = $true)]
    [string] $AppPoolName,

    [Parameter(Mandatory = $true)]
    [string] $DestinationPath,

    [Parameter(Mandatory = $true)]
    [string] $HealthCheckUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'

$dataRoot = 'C:\ProgramData\NailToolInventory'
$workRoot = Join-Path $dataRoot 'deployments'
$backupRoot = Join-Path $dataRoot 'backups'
$logRoot = Join-Path $dataRoot 'logs'

$workPath = Join-Path $workRoot $timestamp
$archivePath = Join-Path $workPath 'NailToolInventory.zip'
$stagingPath = Join-Path $workPath 'staging'
$backupPath = Join-Path $backupRoot $timestamp
$transcriptPath = Join-Path $logRoot "deploy-$timestamp.log"

$backupCreated = $false
$appPoolStopped = $false
$transcriptStarted = $false

function Invoke-DirectoryMirror {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Source,

        [Parameter(Mandatory = $true)]
        [string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        throw "Source directory does not exist: $Source"
    }

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    & robocopy.exe `
        $Source `
        $Destination `
        /MIR `
        /R:3 `
        /W:2 `
        /COPY:DAT `
        /DCOPY:DAT `
        /NFL `
        /NDL `
        /NJH `
        /NJS `
        /NP

    $exitCode = $LASTEXITCODE

    if ($exitCode -ge 8) {
        throw "Robocopy failed with exit code $exitCode while copying '$Source' to '$Destination'."
    }
}

function Stop-ApplicationPool {
    $state = (Get-WebAppPoolState -Name $AppPoolName).Value

    if ($state -ne 'Stopped') {
        Stop-WebAppPool -Name $AppPoolName
    }

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $state = (Get-WebAppPoolState -Name $AppPoolName).Value

        if ($state -eq 'Stopped') {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "Application pool '$AppPoolName' did not stop within 30 seconds."
}

function Start-ApplicationPool {
    $state = (Get-WebAppPoolState -Name $AppPoolName).Value

    if ($state -ne 'Started') {
        Start-WebAppPool -Name $AppPoolName
    }

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $state = (Get-WebAppPoolState -Name $AppPoolName).Value

        if ($state -eq 'Started') {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "Application pool '$AppPoolName' did not start within 30 seconds."
}

function Get-StorageAccessToken {
    $resource = [Uri]::EscapeDataString('https://storage.azure.com/')

    $tokenUri = `
        "http://169.254.169.254/metadata/identity/oauth2/token" +
        "?api-version=2018-02-01&resource=$resource"

    $response = Invoke-RestMethod `
        -Method Get `
        -Uri $tokenUri `
        -Headers @{ Metadata = 'true' } `
        -TimeoutSec 30

    if ([string]::IsNullOrWhiteSpace($response.access_token)) {
        throw 'The VM managed identity did not return a Storage access token.'
    }

    return $response.access_token
}

function Test-LocalApplication {
    $publicUri = [Uri] $HealthCheckUrl

    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $response = $null

        try {
            $request = [Net.HttpWebRequest]::Create('http://127.0.0.1/')
            $request.Method = 'GET'
            $request.Host = $publicUri.Host
            $request.AllowAutoRedirect = $false
            $request.Timeout = 30000

            $response = $request.GetResponse()
            $statusCode = [int] $response.StatusCode

            if ($statusCode -ge 200 -and $statusCode -lt 400) {
                Write-Host "Local health check passed with HTTP $statusCode."
                return
            }
        }
        catch [Net.WebException] {
            if ($null -ne $_.Exception.Response) {
                $response = $_.Exception.Response
                $statusCode = [int] $response.StatusCode

                if ($statusCode -ge 300 -and $statusCode -lt 400) {
                    Write-Host "Local health check passed with HTTP redirect $statusCode."
                    return
                }

                Write-Warning `
                    "Local health check returned HTTP $statusCode (attempt $attempt/30)."
            }
            else {
                Write-Warning `
                    "Local health check failed (attempt $attempt/30): $($_.Exception.Message)"
            }
        }
        finally {
            if ($null -ne $response) {
                $response.Close()
            }
        }

        Start-Sleep -Seconds 10
    }

    throw 'The application did not become healthy within five minutes.'
}

New-Item -ItemType Directory -Path $workPath -Force | Out-Null
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

try {
    Start-Transcript -Path $transcriptPath -Force | Out-Null
    $transcriptStarted = $true

    Write-Host "Starting deployment of '$BlobName' at $(Get-Date -Format o)."

    $token = Get-StorageAccessToken
    $escapedBlobName = [Uri]::EscapeDataString($BlobName)

    $blobUri = `
        "https://$StorageAccount.blob.core.windows.net/" +
        "$ContainerName/$escapedBlobName"

    Invoke-WebRequest `
        -Method Get `
        -Uri $blobUri `
        -Headers @{
            Authorization = "Bearer $token"
            'x-ms-version' = '2023-11-03'
        } `
        -OutFile $archivePath `
        -UseBasicParsing `
        -TimeoutSec 300

    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
        throw "Deployment package was not downloaded: $archivePath"
    }

    if ((Get-Item -LiteralPath $archivePath).Length -eq 0) {
        throw 'The downloaded deployment package is empty.'
    }

    $actualSha256 = `
        (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash

    if ($actualSha256 -ne $PackageSha256) {
        throw `
            "Deployment package SHA-256 mismatch. " +
            "Expected '$PackageSha256', received '$actualSha256'."
    }

    Expand-Archive `
        -LiteralPath $archivePath `
        -DestinationPath $stagingPath `
        -Force

    $webConfigPath = Join-Path $stagingPath 'web.config'

    if (-not (Test-Path -LiteralPath $webConfigPath -PathType Leaf)) {
        throw 'The deployment package does not contain web.config.'
    }

    Import-Module WebAdministration

    if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
        throw "IIS application pool does not exist: $AppPoolName"
    }
        if (Test-Path -LiteralPath $DestinationPath -PathType Container) {
        Write-Host "Backing up current application to '$backupPath'."

        Invoke-DirectoryMirror `
            -Source $DestinationPath `
            -Destination $backupPath

        $backupCreated = $true
    }

    Write-Host "Stopping IIS application pool '$AppPoolName'."
    Stop-ApplicationPool
    $appPoolStopped = $true

    Write-Host "Deploying new application to '$DestinationPath'."

    Invoke-DirectoryMirror `
        -Source $stagingPath `
        -Destination $DestinationPath

    Write-Host "Starting IIS application pool '$AppPoolName'."
    Start-ApplicationPool
    $appPoolStopped = $false

    Test-LocalApplication

    Get-ChildItem -LiteralPath $backupRoot -Directory |
        Sort-Object CreationTime -Descending |
        Select-Object -Skip 5 |
        Remove-Item -Recurse -Force

    Write-Host `
        "DEPLOYMENT_SUCCEEDED: '$BlobName' was deployed successfully."
}

catch {
    $deploymentError = $_

    Write-Host `
        "Deployment failed: $($deploymentError.Exception.Message)" `
        -ForegroundColor Red

    if ($backupCreated) {
        try {
            Write-Warning "Rolling back to '$backupPath'."

            Stop-ApplicationPool

            Invoke-DirectoryMirror `
                -Source $backupPath `
                -Destination $DestinationPath

            Start-ApplicationPool
            $appPoolStopped = $false

            Write-Warning 'Rollback completed.'
        }
        catch {
            Write-Host `
                "Rollback failed: $($_.Exception.Message)" `
                -ForegroundColor Red
        }
    }
    elseif ($appPoolStopped) {
        try {
            Start-ApplicationPool
        }
        catch {
            Write-Host `
                "Unable to restart application pool: $($_.Exception.Message)" `
                -ForegroundColor Red
        }
    }

    throw $deploymentError
}
finally {
    if (Test-Path -LiteralPath $workPath) {
        Remove-Item `
            -LiteralPath $workPath `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }

    if ($transcriptStarted) {
        Stop-Transcript | Out-Null
    }
}
