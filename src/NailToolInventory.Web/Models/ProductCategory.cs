using System.ComponentModel.DataAnnotations;

namespace NailToolInventory.Models;

public enum ProductCategory
{
    [Display(Name = "A-Shape Nipper")]
    AShapeNipper,

    [Display(Name = "Nail Nipper")]
    NailNipper,

    [Display(Name = "Cuticle Pusher")]
    CuticlePusher
}