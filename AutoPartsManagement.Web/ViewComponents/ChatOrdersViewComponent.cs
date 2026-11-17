using System.Security.Claims;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.ViewComponents;

/// <summary>
/// Dải "Đơn gần đây" trong khung chat: link nhanh tới các đơn mới nhất của khách.
/// </summary>
public class ChatOrdersViewComponent : ViewComponent
{
    private const int MaxOrders = 3;

    private readonly IOrderService _orders;

    public ChatOrdersViewComponent(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = new List<Order>();
        if (!string.IsNullOrEmpty(userId))
        {
            try { orders = (await _orders.GetByUserAsync(userId)).Take(MaxOrders).ToList(); }
            catch { orders = new(); } // không để lỗi tải đơn làm hỏng cả trang
        }
        return View(orders);
    }
}
