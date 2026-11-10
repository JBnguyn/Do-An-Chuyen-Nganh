using AutoPartsManagement.Data.Models;

namespace AutoPartsManagement.Web.Helpers;

/// <summary>Biểu tượng và màu cho từng loại thông báo.</summary>
public static class NotificationUi
{
    public static string Icon(string type) => type switch
    {
        NotificationTypes.NewOrder => "bi-bag-check-fill",
        NotificationTypes.OrderCancelled => "bi-x-circle-fill",
        NotificationTypes.LowStock => "bi-exclamation-triangle-fill",
        NotificationTypes.OutOfStock => "bi-box-seam-fill",
        _ => "bi-bell-fill"
    };

    public static string Css(string type) => type switch
    {
        NotificationTypes.NewOrder => "notif-type-order",
        NotificationTypes.OrderCancelled => "notif-type-cancel",
        NotificationTypes.LowStock => "notif-type-low",
        NotificationTypes.OutOfStock => "notif-type-out",
        _ => ""
    };

    public static string TimeAgo(DateTime time)
    {
        var span = DateTime.Now - time;
        if (span.TotalMinutes < 1) return "Vừa xong";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
        return time.ToString("dd/MM/yyyy HH:mm");
    }
}
