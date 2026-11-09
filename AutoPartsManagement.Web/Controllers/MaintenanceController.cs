using System.Security.Claims;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// "Xe của tôi & lịch bảo dưỡng": khách lưu xe, cập nhật số km,
/// xem hạng mục sắp đến hạn và phụ tùng gợi ý.
/// </summary>
public class MaintenanceController : Controller
{
    private readonly IMaintenanceService _service;

    public MaintenanceController(IMaintenanceService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /Maintenance?id=3
    public async Task<IActionResult> Index(int? id)
    {
        var vm = new MaintenanceIndexViewModel { Vehicles = await _service.GetVehiclesAsync(UserId) };

        var selected = vm.Vehicles.FirstOrDefault(v => v.Id == id) ?? vm.Vehicles.FirstOrDefault();
        if (selected != null)
            vm.Summary = await _service.GetSummaryAsync(selected);

        return View(vm);
    }

    // POST: /Maintenance/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewVehicle")] UserVehicle input)
    {
        // UserId lấy từ tài khoản đăng nhập, không lấy từ form
        ModelState.Remove("NewVehicle.UserId");
        ModelState.Remove(nameof(UserVehicle.UserId));

        if (!ModelState.IsValid)
        {
            var vm = new MaintenanceIndexViewModel
            {
                Vehicles = await _service.GetVehiclesAsync(UserId),
                NewVehicle = input
            };
            var first = vm.Vehicles.FirstOrDefault();
            if (first != null) vm.Summary = await _service.GetSummaryAsync(first);
            ViewData["ShowForm"] = true;
            return View(nameof(Index), vm);
        }

        var result = await _service.AddVehicleAsync(UserId, input);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { id = result.Succeeded ? input.Id : (int?)null });
    }

    // POST: /Maintenance/UpdateKm/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateKm(int id, int km)
    {
        var result = await _service.UpdateKmAsync(id, UserId, km);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { id });
    }

    // POST: /Maintenance/MarkDone/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDone(int id, string itemKey, int? km, DateTime? date)
    {
        var result = await _service.MarkDoneAsync(id, UserId, itemKey, km, date);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { id });
    }

    // POST: /Maintenance/Delete/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteVehicleAsync(id, UserId);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
