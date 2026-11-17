using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Cấu hình trợ lý chat: tên bot, xưng hô, giọng điệu, giờ làm việc, chính sách.
/// </summary>
[Authorize(Roles = AppRoles.Admin)] // chỉ Admin
public class BotSettingsController : Controller
{
    private readonly IBotSettingService _service;

    public BotSettingsController(IBotSettingService service)
    {
        _service = service;
    }

    // GET: /BotSettings
    public async Task<IActionResult> Index()
    {
        var setting = await _service.GetAsync();
        return View(setting);
    }

    // POST: /BotSettings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        [Bind("AiEnabled,BotName,BotPronoun,CustomerPronoun,Tone,EmojiLevel,WorkingHours,Policies")] BotSetting input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await _service.SaveAsync(input);
        TempData["Success"] = "Đã lưu cấu hình bot. Tin nhắn tiếp theo của khách sẽ dùng cấu hình mới.";
        return RedirectToAction(nameof(Index));
    }
}