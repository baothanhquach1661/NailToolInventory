namespace NailToolInventory.Models;

public class Product
{
    public int Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ProductCategory Category { get; private set; }

    public decimal CostPrice { get; private set; }

    public decimal SellingPrice { get; private set; }

    public int QuantityOnHand { get; private set; }

    public int ReorderLevel { get; private set; }

    public bool IsActive { get; private set; } = true;


    // EF Core sẽ sử dụng constructor này sau này.
    private Product()
    {
    }


    public Product(
        string sku,
        string name,
        ProductCategory category,
        decimal costPrice,
        decimal sellingPrice,
        int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (costPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(costPrice),
                "Cost price cannot be negative.");

        if (sellingPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(sellingPrice),
                "Selling price cannot be negative.");

        if (reorderLevel < 0)
            throw new ArgumentOutOfRangeException(
                nameof(reorderLevel),
                "Reorder level cannot be negative.");

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        ReorderLevel = reorderLevel;
        QuantityOnHand = 0;
        IsActive = true;
    }


    public void ReceiveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        QuantityOnHand += quantity;
    }


    public void IssueStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");

        if (quantity > QuantityOnHand)
            throw new InvalidOperationException(
                "Not enough inventory to complete this transaction.");

        QuantityOnHand -= quantity;
    }


    public bool IsLowStock()
    {
        return QuantityOnHand <= ReorderLevel;
    }
}