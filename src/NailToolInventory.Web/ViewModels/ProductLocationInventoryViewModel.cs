using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.ViewModels;

public class ProductLocationInventoryViewModel
{
    public int ProductId { get; set; }

    public string ProductSku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public int TotalQuantityOnHand { get; set; }

    public int TotalReservedQuantity { get; set; }

    public int TotalAvailableQuantity =>
        TotalQuantityOnHand - TotalReservedQuantity;

    public List<ProductLocationInventoryItemViewModel> Locations
    { get; set; } = new();
}

public class ProductLocationInventoryItemViewModel
{
    public int InventoryLevelId { get; set; }

    public int InventoryLocationId { get; set; }

    public string LocationCode { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public bool IsLocationActive { get; set; }

    public int QuantityOnHand { get; set; }

    public int ReservedQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public bool IsLowStock { get; set; }
}

public class UpdateReorderLevelViewModel
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int InventoryLevelId { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Reorder level cannot be negative.")]
    public int ReorderLevel { get; set; }
}