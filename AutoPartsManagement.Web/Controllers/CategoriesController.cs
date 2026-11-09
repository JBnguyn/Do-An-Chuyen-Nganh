using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Quản lý Danh mục (loại) phụ tùng.
/// </summary>
[Authorize(Roles = AppRoles.Admin)] // cả trang chỉ dành cho Admin; khách xem hàng ở trang Cửa hàng
public class CategoriesController : Controller
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    // GET: /Categories?search=...&page=1
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = QueryableExtensions.DefaultPageSize)
    {
        ViewData["Search"] = search;
        var result = await _service.GetPagedAsync(search, page, pageSize);
        return View(result);
    }

    // GET: /Categories/Create
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new Category());

    // POST: /Categories/Create
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
    {
        if (!ModelState.IsValid)
            return View(category);

        var result = await _service.CreateAsync(category);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(category);
        }

        TempData["Success"] = $"Đã thêm danh mục \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Categories/Edit/5
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    // POST: /Categories/Edit/5
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
    {
        if (id != category.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View(category);

        var result = await _service.UpdateAsync(category);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(category);
        }

        TempData["Success"] = $"Đã cập nhật danh mục \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Categories/Delete/5
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category == null) return NotFound();

        ViewData["ProductCount"] = await _service.CountProductsAsync(id);
        return View(category);
    }

    // POST: /Categories/Delete/5
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _service.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã xóa danh mục." : result.Message;
        return RedirectToAction(nameof(Index));
    }
}
