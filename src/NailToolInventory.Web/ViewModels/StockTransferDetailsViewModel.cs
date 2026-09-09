using NailToolInventory.Models;

namespace NailToolInventory.ViewModels;

public class StockTransferDetailsViewModel
{
    public int Id { get; set; }

    public string TransferNumber { get; set; } =
        string.Empty;

    public string SourceLocationName { get; set; } =
        string.Empty;

    public string DestinationLocationName { get; set; } =
        string.Empty;

    public StockTransferStatus Status { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ShippedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public List<StockTransferDetailsItemViewModel> Items
    {
        get;
        set;
    } = new();

    public bool CanShip =>
        Status == StockTransferStatus.Draft;

    public bool CanReceive =>
        Status == StockTransferStatus.InTransit ||
        Status == StockTransferStatus.PartiallyReceived;

    public bool CanCancel =>
        Status == StockTransferStatus.Draft;
}


public class StockTransferDetailsItemViewModel
{
    public int ProductId { get; set; }

    public string ProductSku { get; set; } =
        string.Empty;

    public string ProductName { get; set; } =
        string.Empty;

    public int Quantity { get; set; }

    public int QuantityReceived { get; set; }

    public int RemainingQuantity =>
        Quantity - QuantityReceived;
}