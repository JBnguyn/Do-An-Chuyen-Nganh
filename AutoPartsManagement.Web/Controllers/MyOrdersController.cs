using System.Security.Claims;
using AutoPartsManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// "Đơn hàng của tôi": khách xem lịch sử đơn, chi tiết đơn và tự hủy đơn đang chờ xác nhận.
/// </summary>
public class MyOrdersController : Controller
{
    private readonly IOrderService _orders;

    public MyOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /MyOrders
    public async Task<IActionResult> Index()
    {
        return View(await _orders.GetByUserAsync(UserId));
    }

    // GET: /MyOrders/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null || order.Customer?.UserId != UserId) return NotFound();
        return View(order);
    }

    // POST: /MyOrders/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _orders.CancelByUserAsync(id, UserId);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }
}
