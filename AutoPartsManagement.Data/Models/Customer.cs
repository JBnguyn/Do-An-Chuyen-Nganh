using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Khách hàng
/// </summary>
public class Customer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
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

    /// <summary>
    /// Id tài khoản đăng nhập (AspNetUsers.Id) của khách - dùng để biết đơn hàng thuộc tài khoản nào.
    /// Null = khách tạo tay, không gắn tài khoản.
    /// </summary>
    [StringLength(450)]
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
