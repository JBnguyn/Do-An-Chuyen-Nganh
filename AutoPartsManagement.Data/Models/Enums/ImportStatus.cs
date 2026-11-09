namespace AutoPartsManagement.Data.Models.Enums;

/// <summary>
/// Trạng thái phiếu nhập hàng
/// </summary>
public enum ImportStatus
{
    Pending = 0,       // Chờ duyệt
    Approved = 1,      // Đã duyệt
    Received = 2,      // Đã nhận hàng
    Cancelled = 3      // Đã hủy
}
