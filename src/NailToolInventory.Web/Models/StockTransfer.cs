namespace NailToolInventory.Models;

public class StockTransfer
{
    public int Id { get; private set; }

    public string TransferNumber { get; private set; } =
        string.Empty;

    public int SourceLocationId { get; private set; }

    public InventoryLocation SourceLocation
    { get; private set; } = null!;

    public int DestinationLocationId { get; private set; }

    public InventoryLocation DestinationLocation
    { get; private set; } = null!;

    public StockTransferStatus Status { get; private set; }

    public string? Reference { get; private set; }

    public string? Notes { get; private set; }

    public string? CreatedByUserId { get; private set; }

    public ApplicationUser? CreatedByUser
    { get; private set; }

    public string? CreatedByName { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ShippedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public ICollection<StockTransferItem> Items
    { get; private set; } =
        new List<StockTransferItem>();

    private StockTransfer()
    {
    }

    public StockTransfer(
        string transferNumber,
        int sourceLocationId,
        int destinationLocationId,
        string? reference = null,
        string? notes = null,
        string? createdByUserId = null,
        string? createdByName = null)
    {
        if (string.IsNullOrWhiteSpace(transferNumber))
        {
            throw new ArgumentException(
                "Transfer number is required.",
                nameof(transferNumber));
        }

        var normalizedTransferNumber =
            transferNumber.Trim().ToUpperInvariant();

        if (normalizedTransferNumber.Length > 50)
        {
            throw new ArgumentException(
                "Transfer number cannot exceed 50 characters.",
                nameof(transferNumber));
        }

        if (sourceLocationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceLocationId));
        }

        if (destinationLocationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destinationLocationId));
        }

        if (sourceLocationId == destinationLocationId)
        {
            throw new InvalidOperationException(
                "Source and destination locations " +
                "must be different.");
        }

        TransferNumber = normalizedTransferNumber;
        SourceLocationId = sourceLocationId;
        DestinationLocationId = destinationLocationId;

        Reference = NormalizeOptional(
            reference,
            100,
            nameof(reference));

        Notes = NormalizeOptional(
            notes,
            500,
            nameof(notes));

        CreatedByUserId =
            string.IsNullOrWhiteSpace(createdByUserId)
                ? null
                : createdByUserId.Trim();

        CreatedByName = NormalizeOptional(
            createdByName,
            256,
            nameof(createdByName));

        Status = StockTransferStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void AddItem(
        int productId,
        int quantity)
    {
        EnsureDraft();

        if (Items.Any(item =>
            item.ProductId == productId))
        {
            throw new InvalidOperationException(
                "This product is already included " +
                "in the transfer.");
        }

        Items.Add(
            new StockTransferItem(
                productId,
                quantity));
    }

    public void MarkInTransit()
    {
        EnsureDraft();

        if (Items.Count == 0)
        {
            throw new InvalidOperationException(
                "A transfer must contain at least one product.");
        }

        Status = StockTransferStatus.InTransit;
        ShippedAtUtc = DateTime.UtcNow;
    }

    public void RecordReceipt(
        int productId,
        int quantity)
    {
        if (Status != StockTransferStatus.InTransit &&
            Status != StockTransferStatus.PartiallyReceived)
        {
            throw new InvalidOperationException(
                "Only an in-transit transfer can be received.");
        }

        var item = Items.SingleOrDefault(item =>
            item.ProductId == productId);

        if (item is null)
        {
            throw new InvalidOperationException(
                "The product does not belong to this transfer.");
        }

        item.Receive(quantity);

        if (Items.All(item =>
            item.IsFullyReceived))
        {
            Status = StockTransferStatus.Completed;
            CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            Status =
                StockTransferStatus.PartiallyReceived;
        }
    }

    public void Cancel()
    {
        EnsureDraft();

        Status = StockTransferStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }

    private void EnsureDraft()
    {
        if (Status != StockTransferStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft transfer can be modified.");
        }
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalizedValue;
    }
}