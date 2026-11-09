using System.Security.Claims;
using AutoPartsManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.ViewComponents;

/// <summary>
/// Link "Bảo dưỡng xe" trên menu, kèm số hạng mục sắp đến hạn / quá hạn.
/// </summary>
public class MaintenanceBadgeViewComponent : ViewComponent
{
    private readonly IMaintenanceService _service;

    public MaintenanceBadgeViewComponent(IMaintenanceService service)
    {
        _service = service;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var count = 0;
        if (!string.IsNullOrEmpty(userId))
        {
            try { count = await _service.CountDueAsync(userId); }
            catch { count = 0; } // không để lỗi đếm làm hỏng cả trang
        }
        return View(count);
    }
}
