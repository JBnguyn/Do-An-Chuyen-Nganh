using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Quản lý Nhà cung cấp.
/// </summary>
public class SuppliersController : Controller
{
    private readonly ISupplierService _service;

    public SuppliersController(ISupplierService service)
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
    public IActionResult Create() => View(new Supplier());

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Phone,Email,Address")] Supplier supplier)
    {
        if (!ModelState.IsValid)
            return View(supplier);

        var result = await _service.CreateAsync(supplier);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(supplier);
        }

        TempData["Success"] = $"Đã thêm nhà cung cấp \"{supplier.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _service.GetByIdAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email,Address")] Supplier supplier)
    {
        if (id != supplier.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View(supplier);

        var result = await _service.UpdateAsync(supplier);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return View(supplier);
        }

        TempData["Success"] = $"Đã cập nhật nhà cung cấp \"{supplier.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _service.GetByIdAsync(id);
        if (supplier == null) return NotFound();

        ViewData["ImportCount"] = await _service.CountImportsAsync(id);
        return View(supplier);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _service.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã xóa nhà cung cấp." : result.Message;
        return RedirectToAction(nameof(Index));
    }
}
