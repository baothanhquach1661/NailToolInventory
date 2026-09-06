using System.ComponentModel.DataAnnotations.Schema;

namespace NailToolInventory.Models;

public class InventoryLevel
{
    public int Id { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int InventoryLocationId { get; private set; }

    public InventoryLocation Location { get; private set; } =
        null!;

    public int QuantityOnHand { get; private set; }

    public int ReservedQuantity { get; private set; }

    public int ReorderLevel { get; private set; }


    [NotMapped]
    public int AvailableQuantity =>
        QuantityOnHand - ReservedQuantity;


    private InventoryLevel()
    {
    }


    public InventoryLevel(
        int productId,
        int inventoryLocationId,
        int initialQuantity = 0,
        int reorderLevel = 0)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId));
        }

        if (inventoryLocationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inventoryLocationId));
        }

        if (initialQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialQuantity),
                "Initial quantity cannot be negative.");
        }

        ProductId = productId;
        InventoryLocationId = inventoryLocationId;
        QuantityOnHand = initialQuantity;
        ReservedQuantity = 0;

        SetReorderLevel(reorderLevel);
    }


    public void ReceiveStock(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        QuantityOnHand += quantity;
    }


    public void IssueStock(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                "Not enough available inventory.");
        }

        QuantityOnHand -= quantity;
    }


    public void AdjustStock(int newQuantity)
    {
        if (newQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newQuantity),
                "Inventory quantity cannot be negative.");
        }

        if (newQuantity < ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Inventory cannot be adjusted below " +
                "the reserved quantity.");
        }

        QuantityOnHand = newQuantity;
    }


    public void ReserveStock(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > AvailableQuantity)
        {
            throw new InvalidOperationException(
                "Not enough available inventory to reserve.");
        }

        ReservedQuantity += quantity;
    }


    public void ReleaseReservation(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot release more than the reserved quantity.");
        }

        ReservedQuantity -= quantity;
    }


    public void FulfillReservation(int quantity)
    {
        EnsurePositiveQuantity(quantity);

        if (quantity > ReservedQuantity)
        {
            throw new InvalidOperationException(
                "Cannot fulfill more than the reserved quantity.");
        }

        ReservedQuantity -= quantity;
        QuantityOnHand -= quantity;
    }


    public void SetReorderLevel(int reorderLevel)
    {
        if (reorderLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reorderLevel),
                "Reorder level cannot be negative.");
        }

        ReorderLevel = reorderLevel;
    }


    public bool IsLowStock()
    {
        return AvailableQuantity <= ReorderLevel;
    }


    private static void EnsurePositiveQuantity(
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }
    }
}