using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.ViewModels;

public class IssueStockViewModel
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
        ErrorMessage = "Quantity must be greater than zero.")]
    [Display(Name = "Quantity Issued")]
    public int Quantity { get; set; }


    [StringLength(100)]
    [Display(Name = "Reference / Order Number")]
    public string? Reference { get; set; }


    [StringLength(500)]
    public string? Notes { get; set; }
}