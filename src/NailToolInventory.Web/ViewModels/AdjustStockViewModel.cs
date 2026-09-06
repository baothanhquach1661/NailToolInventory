using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NailToolInventory.ViewModels;

public class AdjustStockViewModel
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Please select an inventory location.")]
    [Display(Name = "Inventory Location")]
    public int InventoryLocationId { get; set; }

    public string SelectedLocationName { get; set; } =
        string.Empty;

    public List<SelectListItem> Locations { get; set; } =
        new();

    [Display(Name = "Current On Hand")]
    public int CurrentQuantity { get; set; }

    [Display(Name = "Reserved Quantity")]
    public int ReservedQuantity { get; set; }

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