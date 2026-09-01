using NailToolInventory.Models;

namespace NailToolInventory.ViewModels;

public class DashboardViewModel
{
    public int TotalProducts { get; set; }

    public int TotalUnitsOnHand { get; set; }

    public int LowStockProducts { get; set; }

    public decimal InventoryCostValue { get; set; }

    public IReadOnlyList<InventoryTransaction> RecentTransactions
    { get; set; } = new List<InventoryTransaction>();
}