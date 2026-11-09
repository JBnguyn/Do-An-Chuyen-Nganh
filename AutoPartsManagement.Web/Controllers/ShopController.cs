using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using AutoPartsManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsManagement.Web.Controllers;

/// <summary>
/// Giao diện cửa hàng (web bán hàng): trang chủ, danh sách / tìm kiếm, chi tiết sản phẩm.
/// Chỉ hiển thị phụ tùng đang kinh doanh.
/// </summary>
public class ShopController : Controller
{
    private const int GridPageSize = 12;
    private const int TabSize = 8;
    private const int MaxTabs = 4;

    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IBrandService _brands;

    public ShopController(IProductService products, ICategoryService categories, IBrandService brands)
    {
        _products = products;
        _categories = categories;
        _brands = brands;
    }

    // GET: /  hoặc /Shop
    public async Task<IActionResult> Index()
    {
        var categories = (await _categories.GetPagedAsync(null, 1, 100)).Items.ToList();
        var brands = (await _brands.GetPagedAsync(null, 1, 100)).Items.ToList();

        var vm = new ShopHomeViewModel { Categories = categories, Brands = brands };

        // Tab "Mới nhất"
        var newest = await _products.GetPagedAsync(new ProductFilter { IsActive = true, SortBy = "newest", PageSize = TabSize });
        vm.Tabs.Add(new ShopProductTab { Key = "newest", Title = "Mới nhất", Products = newest.Items });

        // Tab theo các danh mục có nhiều sản phẩm nhất
        foreach (var c in categories.Where(c => c.ProductCount > 0).OrderByDescending(c => c.ProductCount).Take(MaxTabs))
        {
            var items = await _products.GetPagedAsync(new ProductFilter { IsActive = true, CategoryId = c.Id, PageSize = TabSize });
            vm.Tabs.Add(new ShopProductTab { Key = $"cat{c.Id}", Title = c.Name, CategoryId = c.Id, Products = items.Items });
        }

        return View(vm);
    }

    // GET: /Shop/Search?search=&categoryId=&brandId=&stockStatus=&sortBy=&page=
    public async Task<IActionResult> Search([FromQuery] ProductFilter filter)
    {
        filter.IsActive = true;       // khách chỉ thấy hàng đang kinh doanh
        filter.PageSize = GridPageSize;

        var vm = new ShopSearchViewModel
        {
            Filter = filter,
            Result = await _products.GetPagedAsync(filter),
            Categories = (await _categories.GetPagedAsync(null, 1, 100)).Items.ToList(),
            Brands = (await _brands.GetPagedAsync(null, 1, 100)).Items.ToList()
        };
        return View(vm);
    }

    // GET: /Shop/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null || !product.IsActive) return NotFound();

        var related = await _products.GetPagedAsync(new ProductFilter
        {
            IsActive = true,
            CategoryId = product.CategoryId,
            PageSize = 5
        });

        return View(new ShopDetailViewModel
        {
            Product = product,
            Related = related.Items.Where(p => p.Id != product.Id).Take(4).ToList()
        });
    }
}
