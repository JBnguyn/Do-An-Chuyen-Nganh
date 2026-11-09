using AutoPartsManagement.Data.Models.Enums;

namespace AutoPartsManagement.Web.Helpers;

/// <summary>Hiển thị trạng thái đơn hàng bằng tiếng Việt.</summary>
public static class OrderUi
{
    public static string Label(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Processing => "Đang chuẩn bị",
        OrderStatus.Shipped => "Đang giao",
        OrderStatus.Delivered => "Đã giao",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static string Badge(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Processing => "bg-info text-dark",
        OrderStatus.Shipped => "bg-primary",
        OrderStatus.Delivered => "bg-success",
        OrderStatus.Cancelled => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string Icon(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "bi-hourglass-split",
        OrderStatus.Processing => "bi-box-seam",
        OrderStatus.Shipped => "bi-truck",
        OrderStatus.Delivered => "bi-check2-circle",
        OrderStatus.Cancelled => "bi-x-circle",
        _ => "bi-circle"
    };

    /// <summary>Các bước hiển thị trên thanh tiến trình đơn hàng (không gồm Hủy).</summary>
    public static readonly OrderStatus[] Steps =
    {
        OrderStatus.Pending, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered
    };
}
