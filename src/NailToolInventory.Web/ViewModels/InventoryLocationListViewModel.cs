namespace NailToolInventory.ViewModels;

public class InventoryLocationListViewModel
{
    public List<InventoryLocationListItemViewModel> Locations
    { get; set; } = new();
}

public class InventoryLocationListItemViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public bool CanFulfillOnlineOrders { get; set; }

    public int FulfillmentPriority { get; set; }

    public int ProductCount { get; set; }

    public int QuantityOnHand { get; set; }

    public int ReservedQuantity { get; set; }

    public int AvailableQuantity =>
        QuantityOnHand - ReservedQuantity;
}