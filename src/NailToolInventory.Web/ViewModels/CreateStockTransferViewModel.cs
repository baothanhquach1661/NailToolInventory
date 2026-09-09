using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NailToolInventory.ViewModels;

public class CreateStockTransferViewModel
    : IValidatableObject
{
    [Required(ErrorMessage = "Source location is required.")]
    [Display(Name = "Source Location")]
    public int? SourceLocationId { get; set; }

    [Required(ErrorMessage = "Destination location is required.")]
    [Display(Name = "Destination Location")]
    public int? DestinationLocationId { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<CreateStockTransferItemViewModel> Items
    {
        get;
        set;
    } = new()
    {
        new CreateStockTransferItemViewModel()
    };

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Locations
    {
        get;
        set;
    } = new List<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Products
    {
        get;
        set;
    } = new List<SelectListItem>();


    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (SourceLocationId.HasValue &&
            DestinationLocationId.HasValue &&
            SourceLocationId.Value ==
            DestinationLocationId.Value)
        {
            yield return new ValidationResult(
                "Source and destination locations must be different.",
                new[]
                {
                    nameof(DestinationLocationId)
                });
        }

        if (Items.Count == 0)
        {
            yield return new ValidationResult(
                "At least one product is required.",
                new[]
                {
                    nameof(Items)
                });
        }
    }
}


public class CreateStockTransferItemViewModel
{
    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public int? ProductId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage =
            "Quantity must be greater than zero.")]
    public int Quantity { get; set; } = 1;
}