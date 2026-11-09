using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Hãng sản xuất phụ tùng (vd: Bosch, Denso, NGK, Brembo, ...)
/// </summary>
public class Brand
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên hãng là bắt buộc")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Country { get; set; }

    public string? LogoUrl { get; set; }

    // Navigation property
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
