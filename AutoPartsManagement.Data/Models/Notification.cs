using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Thông báo gửi cho Admin: đơn hàng mới, khách hủy đơn, sản phẩm sắp hết / hết hàng.
/// Dùng chung cho tất cả tài khoản Admin.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>Loại thông báo, xem <see cref="NotificationTypes"/>.</summary>
    [Required, StringLength(30)]
    public string Type { get; set; } = NotificationTypes.NewOrder;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Trang mở ra khi bấm vào thông báo (vd "/Orders/Detail/12").</summary>
    [StringLength(300)]
    public string? Link { get; set; }

    public int? OrderId { get; set; }
    public int? ProductId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class NotificationTypes
{
    public const string NewOrder = "order";
    public const string OrderCancelled = "cancel";
    public const string LowStock = "lowstock";
    public const string OutOfStock = "outofstock";
}
