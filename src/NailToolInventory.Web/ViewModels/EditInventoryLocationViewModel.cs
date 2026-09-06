using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.ViewModels;

public class EditInventoryLocationViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    [Display(Name = "Location Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Can fulfill online orders")]
    public bool CanFulfillOnlineOrders { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Fulfillment Priority")]
    public int FulfillmentPriority { get; set; }
}