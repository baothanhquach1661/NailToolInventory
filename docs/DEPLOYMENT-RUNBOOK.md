# Deployment Runbook

This runbook explains how to deploy, verify, troubleshoot, roll back, start, and stop the Nail Tool Inventory production environment.

## Environment Information

| Item | Value |
|---|---|
| Production URL | https://nailtool-inventory-bao.westus2.cloudapp.azure.com |
| Azure Resource Group | `rg-nailtool-erp-lab` |
| Azure VM | `nailtool-web-vm` |
| IIS Website | `NailToolInventory` |
| IIS Application Pool | `NailToolInventoryAppPool` |
| Application Directory | `C:\inetpub\NailToolInventory` |
| Backup Directory | `C:\ProgramData\NailToolInventory\backups` |
| Storage Account | `stnailtooldeploy1868` |
| Blob Container | `deployments` |
| Entra App Registration | `github-nailtool-cd` |
| Deployment Script | `scripts/Deploy-Iis.ps1` |
| GitHub Workflow | `.github/workflows/dotnet.yml` |

## Access Requirements

The operator may require:

- Write access to the GitHub repository.
- Permission to review and merge pull requests.
- Azure access to view the VM and deployment resources.
- Windows Server administrator credentials for RDP troubleshooting.
- Access to GitHub Actions workflow logs.

Never store passwords, SQL connection strings, Storage Account keys, or Azure client secrets in this document.

## Standard Deployment Process

Production deployment is initiated by merging approved code into `main`.

### Step 1: Prepare the Branch

Create a focused branch from the latest `main`:

```bash
git switch main
git pull origin main
git switch -c <branch-name>
```

Examples:

```text
feat/add-inventory-report
fix/transfer-validation
docs/update-deployment-runbook
```

### Step 2: Make and Validate Changes

Before committing application changes, run:

```bash
dotnet test NailToolInventory.sln
git diff --check
git status --short
```

Expected result:

- All tests pass.
- `git diff --check` returns no output.
- Only intended files appear in `git status`.

### Step 3: Commit and Push

```bash
git add <files>
git diff --cached --check
git status --short
git commit -m "<type>: <description>"
git push -u origin <branch-name>
```

Recommended commit types:

| Type | Purpose |
|---|---|
| `feat` | New application behavior |
| `fix` | Bug fix |
| `test` | Test changes |
| `ci` | GitHub Actions changes |
| `docs` | Documentation changes |
| `refactor` | Internal code improvement |
| `chore` | Maintenance work |

### Step 4: Open a Pull Request

On GitHub:

1. Open the pushed branch.
2. Select **Compare & pull request**.
3. Confirm the base branch is `main`.
4. Review **Files changed**.
5. Wait for all required checks.

For a pull request, the workflow should:

- Restore dependencies.
- Build the solution.
- Run all xUnit tests.
- Validate the PowerShell deployment script.
- Skip production deployment.

### Step 5: Merge the Pull Request

Merge only when:

- Build and tests are green.
- PowerShell validation passes.
- There are no merge conflicts.
- The changed files have been reviewed.
- No credentials or generated deployment files are included.

A merge into `main` starts the production deployment workflow.

## Automated Production Deployment

The GitHub Actions workflow performs the following sequence:

1. Check out the repository.
2. Install the required .NET SDK.
3. Restore dependencies.
4. Build the solution in Release configuration.
5. Run the automated tests.
6. Validate `Deploy-Iis.ps1`.
7. Publish the web application for `win-x64`.
8. Create a ZIP deployment package.
9. Generate a SHA-256 checksum.
10. Upload the workflow artifact.
11. Authenticate to Azure with GitHub OIDC.
12. Upload the package to private Blob Storage.
13. Start the Azure VM if necessary.
14. Execute Azure VM Run Command.
15. Run the PowerShell deployment script inside Windows Server.
16. Perform the public website health check.
17. Sign out of Azure.

## Monitoring a Deployment

Open:

```text
GitHub repository → Actions → .NET CI/CD
```

Select the workflow run created by the merge into `main`.

Review these areas:

- Build and Test
- Publish application
- Sign in to Azure with OIDC
- Upload deployment package
- Start Azure VM
- Deploy package to IIS
- Verify public website
- Sign out of Azure

A successful deployment should display green checks for all required jobs.

The VM deployment output should include:

```text
DEPLOYMENT_SUCCEEDED
```

## Post-Deployment Verification

After GitHub Actions succeeds, verify the production application manually.

### Public Checks

Open:

```text
https://nailtool-inventory-bao.westus2.cloudapp.azure.com
```

Confirm:

- HTTPS loads without a certificate warning.
- The login page loads.
- An authorized user can sign in.
- The dashboard loads.
- Products and inventory records can be viewed.
- Role-restricted pages behave correctly.
- No unexpected HTTP 500 error appears.

Avoid changing production inventory merely to test the website unless test data is clearly identified.

### GitHub Checks

Confirm:

- The workflow is green.
- The deployed commit matches the latest commit on `main`.
- The CI/CD badge reports `passing`.
- No sensitive values appear in workflow logs.

## Windows Server Health Checks

If the public website fails, connect to the VM through RDP and open PowerShell as Administrator.

### Check the IIS Website

```powershell
Import-Module WebAdministration

Get-Website -Name "NailToolInventory" |
    Select-Object Name, State, PhysicalPath
```

Expected state:

```text
Started
```

### Check the Application Pool

```powershell
Get-WebAppPoolState -Name "NailToolInventoryAppPool"
```

Expected state:

```text
Started
```

### Test IIS Locally

```powershell
curl.exe -k --resolve nailtool-inventory-bao.westus2.cloudapp.azure.com:443:127.0.0.1 `
    https://nailtool-inventory-bao.westus2.cloudapp.azure.com/
```

An HTTP 200 or expected redirect indicates that IIS can reach the application. An HTTP 500 response requires application-level troubleshooting.

### Confirm the SQL Configuration Exists

This command checks whether the machine-level environment variable exists without printing the connection string:

```powershell
[bool][Environment]::GetEnvironmentVariable(
    "ConnectionStrings__DefaultConnection",
    "Machine"
)
```

Expected result:

```text
True
```

Do not print or capture the complete production connection string in screenshots or logs.

## Windows Event Viewer

For an IIS or ASP.NET Core startup failure:

1. Open **Event Viewer**.
2. Go to **Windows Logs → Application**.
3. Sort by date and time.
4. Review recent Error events.
5. Focus on these sources:

   - `.NET Runtime`
   - `IIS AspNetCore Module V2`
   - `Application Error`

PowerShell can also show recent relevant events:

```powershell
Get-WinEvent -FilterHashtable @{
    LogName = "Application"
    StartTime = (Get-Date).AddMinutes(-30)
} |
    Where-Object {
        $_.ProviderName -in @(
            ".NET Runtime",
            "IIS AspNetCore Module V2",
            "Application Error"
        )
    } |
    Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message
```

## Automatic Rollback

The deployment script creates a backup before replacing the current IIS application.

If the new version does not become healthy:

1. The deployment health check fails.
2. The script stops the application pool.
3. The previous application backup is restored.
4. The application pool is restarted.
5. GitHub Actions receives a failure.
6. The production site returns to the previous release when rollback succeeds.

The five most recent backups are retained.

### Inspect Available Backups

```powershell
Get-ChildItem "C:\ProgramData\NailToolInventory\backups" -Directory |
    Sort-Object LastWriteTime -Descending |
    Select-Object Name, LastWriteTime, FullName
```

## Manual Rollback

Manual rollback should be used only when automatic rollback did not restore the application.

Before continuing:

- Use an elevated PowerShell window.
- Confirm the exact backup directory.
- Confirm that the backup contains the published application.
- Do not guess the backup path.
- Record the currently deployed commit and failure details.

Set the verified paths:

```powershell
$backupPath = "C:\ProgramData\NailToolInventory\backups\<verified-backup>"
$appPath = "C:\inetpub\NailToolInventory"
$appPool = "NailToolInventoryAppPool"
```

Validate the paths:

```powershell
if (-not (Test-Path -LiteralPath $backupPath)) {
    throw "The selected backup directory does not exist."
}

if (-not (Test-Path -LiteralPath $appPath)) {
    throw "The IIS application directory does not exist."
}
```

Stop the application pool:

```powershell
Stop-WebAppPool -Name $appPool
```

Restore the verified backup:

```powershell
robocopy $backupPath $appPath /MIR /R:2 /W:2

if ($LASTEXITCODE -gt 7) {
    throw "Robocopy rollback failed with exit code $LASTEXITCODE."
}
```

Start the application pool:

```powershell
Start-WebAppPool -Name $appPool
```

Verify the application locally and publicly after the rollback.

`robocopy /MIR` removes destination files that are not present in the selected backup. Run it only after confirming both paths.

## Common Failure Scenarios

### Build or Test Failure

Symptoms:

- Failure occurs before Azure login.
- No deployment package is uploaded.
- Production is unchanged.

Actions:

1. Open the failed build or test step.
2. Identify the first meaningful error.
3. Reproduce it locally.
4. Fix it on the feature branch.
5. Push a new commit.

### PowerShell Validation Failure

Symptoms:

- The deployment script parser reports syntax errors.
- Production deployment does not begin.

Actions:

1. Open the line reported by GitHub Actions.
2. Check brackets, parentheses, quotes, and YAML indentation.
3. Run local validation.
4. Commit the correction.

### OIDC Login Failure

Symptoms:

- Azure login fails.
- No client secret is involved.

Check:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- Entra federated credential
- Repository name
- GitHub organization or owner
- Allowed branch
- Azure RBAC assignments

The federated credential must match the GitHub repository and deployment branch exactly.

### Blob Upload Failure

Check:

- Storage Account name.
- Container name.
- Storage Blob Data Contributor assignment.
- RBAC assignment scope.
- Azure login success.
- Whether RBAC changes have finished propagating.

### VM Run Command Failure

Check:

- VM power state.
- Azure VM Agent health.
- Virtual Machine Contributor assignment.
- PowerShell error output.
- Available disk space.
- Deployment script parameters.

### Blob Download Failure Inside the VM

Check:

- VM System-Assigned Managed Identity is enabled.
- Managed Identity has Storage Blob Data Reader.
- The role is scoped to the correct container.
- Storage Account and Blob names are correct.
- The package exists.
- RBAC propagation has completed.

### Checksum Failure

Do not bypass checksum verification.

Check:

- The expected SHA-256 value.
- The uploaded ZIP package.
- Whether the package was replaced after its checksum was generated.

Generate a new package and checksum through GitHub Actions.

### IIS HTTP 500

Check:

- Event Viewer Application log.
- IIS website state.
- Application pool state.
- Installed .NET Hosting Bundle.
- Machine-level SQL connection configuration.
- Azure SQL availability.
- Application startup exceptions.

### Azure SQL Timeout

Common indicators:

- SQL error number `-2`.
- `Connection Timeout Expired`.
- Failure during `IdentitySeeder`.
- IIS returns HTTP 500 during startup.

Check:

- Azure SQL status.
- SQL firewall configuration.
- VM network connectivity.
- Connection string availability.
- Credentials and database name.

The application includes an extended connection timeout and retry policy, but persistent configuration or network failures still require correction.

### Local Check Passes but Public Check Fails

Check:

- VM public IP.
- Azure DNS label.
- Network Security Group port 443.
- IIS HTTPS binding.
- Certificate expiration.
- Hostname in the IIS binding.
- Public DNS resolution.

## Certificate Checks

On the Windows VM, review the certificates:

```powershell
Get-ChildItem Cert:\LocalMachine\WebHosting, Cert:\LocalMachine\My `
    -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Subject -like "*nailtool-inventory-bao*"
    } |
    Select-Object Subject, NotAfter, Thumbprint
```

Review the win-acme scheduled task:

```powershell
Get-ScheduledTask |
    Where-Object {
        $_.TaskName -like "*win-acme*"
    } |
    Select-Object TaskName, State
```

If renewal fails, review win-acme logs before manually replacing the IIS binding.

## Starting the Production VM

### Azure Portal

1. Open Azure Portal.
2. Open `nailtool-web-vm`.
3. Select **Start**.
4. Wait for `Running`.
5. Allow IIS and Azure SQL startup time.
6. Test the production URL.

### Azure CLI

```bash
az vm start \
  --resource-group rg-nailtool-erp-lab \
  --name nailtool-web-vm
```

The first website request may be slower while Windows, IIS, ASP.NET Core, and Azure SQL initialize.

## Stopping the Production VM

Use **Stop** from Azure Portal and confirm:

```text
Stopped (deallocated)
```

Azure CLI:

```bash
az vm deallocate \
  --resource-group rg-nailtool-erp-lab \
  --name nailtool-web-vm
```

Shutting down Windows from inside RDP may leave the VM in `Stopped (allocated)` state. Use Azure deallocation when the goal is to stop compute billing.

The website is unavailable while the VM is deallocated.

## Cost-Control Checklist

When the public demo is not required:

- Deallocate the VM.
- Confirm the final Azure power state.
- Review Azure Cost Analysis.
- Keep a Cost Management budget alert enabled.
- Remember that disks, Azure SQL, Storage, and networking can still incur charges.
- Start the VM before interviews or portfolio demonstrations.

The current deployment workflow starts the VM when deploying to `main` and does not automatically stop it afterward.

After merging any pull request into `main`, check whether the VM should remain running.

## Documentation-Only Changes

The current workflow deploys every push to `main`, including documentation-only changes.

Until path filtering is implemented:

1. Expect a documentation merge to start the VM.
2. Monitor the deployment workflow.
3. Verify that production remains healthy.
4. Deallocate the VM afterward if the demo does not need to remain online.

## Final Operational Checklist

Before considering a production release complete:

- [ ] Pull request checks passed.
- [ ] Changes were reviewed.
- [ ] No secrets were committed.
- [ ] Deployment workflow passed.
- [ ] `DEPLOYMENT_SUCCEEDED` appeared.
- [ ] Public HTTPS endpoint responded.
- [ ] Login worked.
- [ ] Dashboard loaded.
- [ ] No new critical Event Viewer errors appeared.
- [ ] Production commit matched `main`.
- [ ] VM cost decision was made after deployment.