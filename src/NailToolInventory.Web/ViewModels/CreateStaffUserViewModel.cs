using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.ViewModels;

public class CreateStaffUserViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;


    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;


    [Required]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage =
            "Password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;


    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(
        nameof(Password),
        ErrorMessage =
            "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}