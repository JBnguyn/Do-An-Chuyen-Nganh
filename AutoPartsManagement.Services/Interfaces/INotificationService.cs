using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;

namespace AutoPartsManagement.Services.Interfaces;

/// <summary>Thông báo cho Admin (đơn mới, hủy đơn, sắp hết hàng).</summary>
public interface INotificationService
{
    /// <summary>Các thông báo mới nhất (cho chuông thông báo).</summary>
    Task<List<Notification>> GetLatestAsync(int take = 8);

    Task<int> CountUnreadAsync();

    Task<PagedResult<Notification>> GetPagedAsync(bool unreadOnly, int page, int pageSize);

    /// <summary>Đánh dấu đã đọc, trả về thông báo (để chuyển tới trang liên quan).</summary>
    Task<Notification?> MarkReadAsync(int id);

    Task<int> MarkAllReadAsync();
}
