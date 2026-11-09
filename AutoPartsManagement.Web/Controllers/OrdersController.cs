using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Quản lý đơn hàng (chỉ Admin): xem danh sách, lọc theo trạng thái, xem chi tiết, đổi trạng thái.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class OrdersController : Controller
{
    private const int PageSize = 15;
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    // GET: /Orders?status=Pending&search=&page=1
    public async Task<IActionResult> Index(OrderStatus? status, string? search, int page = 1)
    {
        ViewData["Status"] = status;
        ViewData["Search"] = search;
        ViewData["Counts"] = await _orders.CountByStatusAsync();
        return View(await _orders.GetPagedAsync(status, search, page, PageSize));
    }

    // GET: /Orders/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    // POST: /Orders/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var result = await _orders.UpdateStatusAsync(id, status);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message ?? "Không có thay đổi.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
