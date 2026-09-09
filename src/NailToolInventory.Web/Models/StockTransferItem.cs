using System.ComponentModel.DataAnnotations.Schema;

namespace NailToolInventory.Models;

public class StockTransferItem
{
    public int Id { get; private set; }

    public int StockTransferId { get; private set; }

    public StockTransfer StockTransfer
    { get; private set; } = null!;

    public int ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int Quantity { get; private set; }

    public int QuantityReceived { get; private set; }

    [NotMapped]
    public int RemainingQuantity =>
        Quantity - QuantityReceived;

    [NotMapped]
    public bool IsFullyReceived =>
        QuantityReceived == Quantity;

    private StockTransferItem()
    {
    }

    public StockTransferItem(
        int productId,
        int quantity)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Transfer quantity must be greater than zero.");
        }

        ProductId = productId;
        Quantity = quantity;
        QuantityReceived = 0;
    }

    public void Receive(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Received quantity must be greater than zero.");
        }

        if (quantity > RemainingQuantity)
        {
            throw new InvalidOperationException(
                "Received quantity cannot exceed " +
                "the remaining transfer quantity.");
        }

        QuantityReceived += quantity;
    }
}