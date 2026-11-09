using System.Security.Claims;
using System.Text.Json;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using AutoPartsManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Đặt hàng: khách điền thông tin nhận hàng, giỏ hàng (lưu trên trình duyệt) được gửi kèm.
/// </summary>
public class CheckoutController : Controller
{
    private readonly IOrderService _orders;

    public CheckoutController(IOrderService orders)
    {
        _orders = orders;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /Checkout
    public async Task<IActionResult> Index()
    {
        // Điền sẵn thông tin lần đặt trước
        var customer = await _orders.GetCustomerByUserAsync(UserId);
        var vm = new CheckoutViewModel
        {
            Name = customer?.Name ?? string.Empty,
            Phone = customer?.Phone ?? string.Empty,
            Email = customer?.Email,
            Address = customer?.Address ?? string.Empty
        };
        return View(vm);
    }

    // POST: /Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel vm)
    {
        var lines = ParseCart(vm.CartJson);
        if (lines.Count == 0)
            ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống.");

        if (!ModelState.IsValid)
            return View(vm);

        var info = new CheckoutInfo
        {
            Name = vm.Name,
            Phone = vm.Phone,
            Email = vm.Email,
            Address = vm.Address,
            Notes = vm.Notes
        };

        var (result, orderId) = await _orders.PlaceOrderAsync(UserId, info, lines);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            return View(vm);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Success), new { id = orderId });
    }

    // GET: /Checkout/Success/5
    public async Task<IActionResult> Success(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null || order.Customer?.UserId != UserId) return NotFound();
        return View(order);
    }

    private static List<CartLine> ParseCart(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<CartLine>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
