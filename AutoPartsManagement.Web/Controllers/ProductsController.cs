using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using AutoPartsManagement.Web.Models;
using AutoPartsManagement.Web.Services;
using AutoPartsManagement.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Quản lý Danh mục Phụ tùng: xem danh sách (tìm kiếm, lọc, sắp xếp, phân trang), thêm, sửa, xóa.
/// </summary>
public class ProductsController : Controller
{
    private const string ImageFolder = "products";

    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IBrandService _brands;
    private readonly IImageStorage _images;

    public ProductsController(
        IProductService products,
        ICategoryService categories,
        IBrandService brands,
        IImageStorage images)
    {
        _products = products;
        _categories = categories;
        _brands = brands;
        _images = images;
    }

    // GET: /Products?search=&categoryId=&brandId=&stockStatus=&isActive=&sortBy=&page=
    public async Task<IActionResult> Index([FromQuery] ProductFilter filter)
    {
        var vm = new ProductIndexViewModel
        {
            Filter = filter,
            Result = await _products.GetPagedAsync(filter),
            Stats = await _products.GetStatsAsync(),
            Categories = new SelectList(await _categories.GetAllAsync(), nameof(Category.Id), nameof(Category.Name), filter.CategoryId),
            Brands = new SelectList(await _brands.GetAllAsync(), nameof(Brand.Id), nameof(Brand.Name), filter.BrandId)
        };
        return View(vm);
    }

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    // GET: /Products/Create
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new Product { IsActive = true });
    }

    // POST: /Products/Create
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,PartNumber,CategoryId,BrandId,Price,Stock,Description,ImageUrl,WarrantyMonths,IsActive")] Product product,
        IFormFile? imageFile)
    {
        ValidateImage(imageFile);
        if (!ModelState.IsValid)
            return await FormViewAsync(product);

        string? uploadedUrl = null;
        if (imageFile is { Length: > 0 })
        {
            uploadedUrl = await _images.SaveAsync(imageFile, ImageFolder);
            product.ImageUrl = uploadedUrl;
        }

        var result = await _products.CreateAsync(product);
        if (!result.Succeeded)
        {
            _images.Delete(uploadedUrl);
            product.ImageUrl = null;
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return await FormViewAsync(product);
        }

        TempData["Success"] = $"Đã thêm phụ tùng \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Products/Edit/5
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null) return NotFound();

        await LoadDropdownsAsync(product.CategoryId, product.BrandId);
        return View(product);
    }

    // POST: /Products/Edit/5
    // Không bind Stock: tồn kho chỉ thay đổi qua phiếu nhập / phiếu xuất.
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,PartNumber,CategoryId,BrandId,Price,Description,ImageUrl,WarrantyMonths,IsActive")] Product product,
        IFormFile? imageFile)
    {
        if (id != product.Id) return BadRequest();

        var existing = await _products.GetByIdAsync(id);
        if (existing == null) return NotFound();
        product.Stock = existing.Stock; // chỉ để hiển thị lại trên form

        ValidateImage(imageFile);
        if (!ModelState.IsValid)
            return await FormViewAsync(product);

        string? uploadedUrl = null;
        if (imageFile is { Length: > 0 })
        {
            uploadedUrl = await _images.SaveAsync(imageFile, ImageFolder);
            product.ImageUrl = uploadedUrl;
        }

        var result = await _products.UpdateAsync(product);
        if (!result.Succeeded)
        {
            _images.Delete(uploadedUrl);
            product.ImageUrl = existing.ImageUrl;
            ModelState.AddModelError(result.Field ?? string.Empty, result.Message!);
            return await FormViewAsync(product);
        }

        // Ảnh cũ không còn dùng nữa -> xóa file
        if (!string.Equals(existing.ImageUrl, product.ImageUrl, StringComparison.OrdinalIgnoreCase))
            _images.Delete(existing.ImageUrl);

        TempData["Success"] = $"Đã cập nhật phụ tùng \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Products/Delete/5
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    // POST: /Products/Delete/5
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _products.GetByIdAsync(id);
        var (result, hardDeleted) = await _products.DeleteAsync(id);

        if (result.Succeeded && hardDeleted)
            _images.Delete(product?.ImageUrl);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ===== Hàm hỗ trợ =====

    private void ValidateImage(IFormFile? imageFile)
    {
        if (imageFile == null) return;
        var error = _images.Validate(imageFile);
        if (error != null)
            ModelState.AddModelError("imageFile", error);
    }

    private async Task LoadDropdownsAsync(int? categoryId = null, int? brandId = null)
    {
        ViewBag.Categories = new SelectList(await _categories.GetAllAsync(), nameof(Category.Id), nameof(Category.Name), categoryId);
        ViewBag.Brands = new SelectList(await _brands.GetAllAsync(), nameof(Brand.Id), nameof(Brand.Name), brandId);
    }

    private async Task<IActionResult> FormViewAsync(Product product)
    {
        await LoadDropdownsAsync(product.CategoryId, product.BrandId);
        return View(product);
    }
}
