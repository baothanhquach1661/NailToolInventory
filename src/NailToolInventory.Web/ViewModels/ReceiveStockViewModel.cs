using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NailToolInventory.ViewModels;

public class ReceiveStockViewModel
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Display(Name = "SKU")]
    public string ProductSku { get; set; } = string.Empty;

    [Display(Name = "Product")]
    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Current Inventory")]
    public int CurrentQuantity { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Please select a receiving location.")]
    [Display(Name = "Receiving Location")]
    public int InventoryLocationId { get; set; }

    public List<SelectListItem> Locations { get; set; } =
        new();

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Quantity must be greater than zero.")]
    [Display(Name = "Quantity Received")]
    public int Quantity { get; set; }

    [StringLength(100)]
    [Display(Name = "Reference / PO Number")]
    public string? Reference { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}