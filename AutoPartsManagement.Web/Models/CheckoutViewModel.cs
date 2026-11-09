using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Web.Models;

/// <summary>Form thông tin nhận hàng ở trang Đặt hàng.</summary>
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [StringLength(200, ErrorMessage = "Họ tên tối đa {1} ký tự.")]
    [Display(Name = "Họ tên người nhận")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^(\+?84|0)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ (vd: 0912345678).")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100)]
    [Display(Name = "Email (không bắt buộc)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
    [StringLength(300, ErrorMessage = "Địa chỉ tối đa {1} ký tự.")]
    [Display(Name = "Địa chỉ giao hàng")]
    public string Address { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ghi chú tối đa {1} ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    /// <summary>Giỏ hàng dạng JSON [{productId, quantity}] do JavaScript điền trước khi gửi.</summary>
    public string? CartJson { get; set; }
}
