using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Nhà cung cấp phụ tùng
/// </summary>
public class Supplier
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    public virtual ICollection<Import> Imports { get; set; } = new List<Import>();
}
