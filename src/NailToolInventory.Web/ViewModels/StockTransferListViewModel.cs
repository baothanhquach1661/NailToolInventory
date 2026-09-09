using NailToolInventory.Models;

namespace NailToolInventory.ViewModels;

public class StockTransferListViewModel
{
    public List<StockTransferListItemViewModel> Transfers
    {
        get;
        set;
    } = new();
}


public class StockTransferListItemViewModel
{
    public int Id { get; set; }

    public string TransferNumber { get; set; } =
        string.Empty;

    public string SourceLocationName { get; set; } =
        string.Empty;

    public string DestinationLocationName { get; set; } =
        string.Empty;

    public StockTransferStatus Status { get; set; }

    public int ProductCount { get; set; }

    public int TotalQuantity { get; set; }

    public int TotalReceived { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}