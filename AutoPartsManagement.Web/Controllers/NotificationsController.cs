using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Helpers;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Thông báo cho Admin: đơn hàng mới, khách hủy đơn, sản phẩm sắp hết / hết hàng.
/// Chuông thông báo trên menu tự hỏi lại server mỗi 15 giây (Feed) để cập nhật.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class NotificationsController : Controller
{
    private const int PageSize = 15;
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(bool unread = false, int page = 1)
    {
        ViewData["Unread"] = unread;
        ViewData["UnreadCount"] = await _notifications.CountUnreadAsync();
        return View(await _notifications.GetPagedAsync(unread, page, PageSize));
    }

    /// <summary>Dữ liệu cho chuông thông báo (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Feed()
    {
        var items = await _notifications.GetLatestAsync(8);
        return Json(new
        {
            unread = await _notifications.CountUnreadAsync(),
            latestId = items.Count > 0 ? items[0].Id : 0,
            items = items.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                isRead = n.IsRead,
                icon = NotificationUi.Icon(n.Type),
                css = NotificationUi.Css(n.Type),
                time = NotificationUi.TimeAgo(n.CreatedAt),
                url = Url.Action(nameof(Open), new { id = n.Id })
            })
        });
    }

    /// <summary>Bấm vào thông báo: đánh dấu đã đọc rồi mở trang liên quan (đơn hàng / nhập kho).</summary>
    public async Task<IActionResult> Open(int id)
    {
        var n = await _notifications.MarkReadAsync(id);
        if (n?.Link != null && Url.IsLocalUrl(n.Link))
            return LocalRedirect(n.Link);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var count = await _notifications.MarkAllReadAsync();
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { ok = true, count });

        TempData["Success"] = count > 0 ? $"Đã đánh dấu {count} thông báo là đã đọc." : "Không có thông báo chưa đọc.";
        return RedirectToAction(nameof(Index));
    }
}
