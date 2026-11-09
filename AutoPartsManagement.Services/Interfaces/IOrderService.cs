using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Tạo đơn hàng từ giỏ hàng. Giá lấy lại từ database, kiểm tra tồn kho và trừ tồn kho.
    /// Trả về Id đơn hàng mới (0 nếu thất bại).
    /// </summary>
    Task<(ServiceResult Result, int OrderId)> PlaceOrderAsync(string userId, CheckoutInfo info, IReadOnlyList<CartLine> lines);

    /// <summary>Thông tin khách đã lưu của tài khoản (để điền sẵn form đặt hàng).</summary>
    Task<Customer?> GetCustomerByUserAsync(string userId);

    /// <summary>Danh sách đơn của một tài khoản, mới nhất trước.</summary>
    Task<List<Order>> GetByUserAsync(string userId);

    /// <summary>Chi tiết đơn (kèm khách hàng và sản phẩm).</summary>
    Task<Order?> GetByIdAsync(int id);

    /// <summary>Danh sách đơn cho Admin: lọc theo trạng thái, tìm theo mã đơn / tên / SĐT.</summary>
    Task<PagedResult<Order>> GetPagedAsync(OrderStatus? status, string? search, int page, int pageSize);

    /// <summary>Số đơn theo từng trạng thái.</summary>
    Task<Dictionary<OrderStatus, int>> CountByStatusAsync();

    /// <summary>Admin đổi trạng thái đơn. Hủy đơn sẽ hoàn lại tồn kho.</summary>
    Task<ServiceResult> UpdateStatusAsync(int id, OrderStatus newStatus);

    /// <summary>Khách tự hủy đơn của mình (chỉ khi đơn còn "Chờ xác nhận").</summary>
    Task<ServiceResult> CancelByUserAsync(int id, string userId);
}
