using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.ViewModels;

public class AdjustStockViewModel
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Current Inventory")]
    public int CurrentQuantity { get; set; }

    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Counted quantity cannot be negative.")]
    [Display(Name = "Counted Quantity")]
    public int NewQuantity { get; set; }

    [Required(ErrorMessage = "Reference is required.")]
    [StringLength(100)]
    public string Reference { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }
}