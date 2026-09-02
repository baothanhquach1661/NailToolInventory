using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.ViewModels;

public class ResetStaffPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;


    [Required]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage =
            "Password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;


    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage =
            "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}