using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>Trang tổng quan thống kê cho Admin.</summary>
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private static readonly int[] AllowedDays = { 7, 30, 90 };
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    // GET: /Dashboard?days=30
    public async Task<IActionResult> Index(int days = 30)
    {
        if (!AllowedDays.Contains(days)) days = 30;
        return View(await _dashboard.GetAsync(days));
    }
}
