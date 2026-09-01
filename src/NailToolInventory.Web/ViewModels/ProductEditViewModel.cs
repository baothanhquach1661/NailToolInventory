using System.ComponentModel.DataAnnotations;
using NailToolInventory.Models;

namespace NailToolInventory.ViewModels;

public class ProductEditViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }


    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;


    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;


    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public ProductCategory? Category { get; set; }


    [Range(
        typeof(decimal),
        "0",
        "999999.99",
        ErrorMessage = "Cost price cannot be negative.")]
    [Display(Name = "Cost Price")]
    public decimal CostPrice { get; set; }


    [Range(
        typeof(decimal),
        "0",
        "999999.99",
        ErrorMessage = "Selling price cannot be negative.")]
    [Display(Name = "Selling Price")]
    public decimal SellingPrice { get; set; }


    [Range(
        0,
        int.MaxValue,
        ErrorMessage = "Reorder level cannot be negative.")]
    [Display(Name = "Reorder Level")]
    public int ReorderLevel { get; set; }


    [Display(Name = "Active Product")]
    public bool IsActive { get; set; }
}