namespace NailToolInventory.Models;

public class InventoryTransaction
{
    public int Id { get; private set; }

    public int ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public InventoryTransactionType Type { get; private set; }

    public int Quantity { get; private set; }

    public int QuantityBefore { get; private set; }

    public int QuantityAfter { get; private set; }

    public string? Reference { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }


    private InventoryTransaction()
    {
    }


    public InventoryTransaction(
        int productId,
        InventoryTransactionType type,
        int quantity,
        int quantityBefore,
        int quantityAfter,
        string? reference = null,
        string? notes = null)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        if (quantityBefore < 0 || quantityAfter < 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantityBefore),
                "Inventory cannot be negative.");

        ProductId = productId;
        Type = type;
        Quantity = quantity;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        Reference = string.IsNullOrWhiteSpace(reference)
            ? null
            : reference.Trim();
        Notes = string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }
}