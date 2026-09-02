using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(100)]
    public string? FullName { get; set; }
}