namespace AutoPartsManagement.Data.Models.Enums;

/// <summary>
/// Trạng thái đơn hàng
/// </summary>
public enum OrderStatus
{
    Pending = 0,       // Chờ xử lý
    Processing = 1,    // Đang xử lý
    Shipped = 2,       // Đã giao hàng
    Delivered = 3,     // Đã nhận hàng
    Cancelled = 4      // Đã hủy
}
