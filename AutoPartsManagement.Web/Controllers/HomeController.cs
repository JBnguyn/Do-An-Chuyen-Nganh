using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoPartsManagement.Web.Models;

namespace AutoPartsManagement.Web.Controllers;

public class HomeController : Controller
{
    // Trang chủ chuyển sang giao diện cửa hàng
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Shop");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
