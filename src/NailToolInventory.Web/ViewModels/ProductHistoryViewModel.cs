using NailToolInventory.Models;

namespace NailToolInventory.ViewModels;

public class ProductHistoryViewModel
{
    public int ProductId { get; set; }

    public string ProductSku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int CurrentQuantity { get; set; }

    public IReadOnlyList<InventoryTransaction> Transactions
    { get; set; } = new List<InventoryTransaction>();
}