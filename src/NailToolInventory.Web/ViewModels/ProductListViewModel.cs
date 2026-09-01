using NailToolInventory.Models;

namespace NailToolInventory.ViewModels;

public class ProductListViewModel
{
    public string? SearchTerm { get; set; }

    public ProductCategory? Category { get; set; }

    public bool LowStockOnly { get; set; }

    public bool IncludeInactive { get; set; }

    public IReadOnlyList<Product> Products
    { get; set; } = new List<Product>();
}