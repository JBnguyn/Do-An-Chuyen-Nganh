using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Quản lý Hãng sản xuất phụ tùng.
/// </summary>
public class BrandsController : Controller
{
    private readonly IBrandService _service;

    public BrandsController(IBrandService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = QueryableExtensions.DefaultPageSize)
    {
        ViewData["Search"] = search;
        var result = await _service.GetPagedAsync(search, page, pageSize);
        return View(result);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new Brand());

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Country,LogoUrl")] Brand brand)
    {
        if (!ModelState.IsValid)
            return View(brand);

        var result = await _service.CreateAsync(brand);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(brand);
        }

        TempData["Success"] = $"Đã thêm hãng \"{brand.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var brand = await _service.GetByIdAsync(id);
        if (brand == null) return NotFound();
        return View(brand);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Country,LogoUrl")] Brand brand)
    {
        if (id != brand.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View(brand);

        var result = await _service.UpdateAsync(brand);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(brand);
        }

        TempData["Success"] = $"Đã cập nhật hãng \"{brand.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _service.GetByIdAsync(id);
        if (brand == null) return NotFound();

        ViewData["ProductCount"] = await _service.CountProductsAsync(id);
        return View(brand);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _service.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã xóa hãng sản xuất." : result.Message;
        return RedirectToAction(nameof(Index));
    }
}
