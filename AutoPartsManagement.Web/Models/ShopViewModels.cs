using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Web.Models;

/// <summary>Dữ liệu trang chủ cửa hàng.</summary>
public class ShopHomeViewModel
{
    public List<CategoryListItem> Categories { get; set; } = new();
    public List<BrandListItem> Brands { get; set; } = new();

    /// <summary>Tab sản phẩm: "Mới nhất" + một số danh mục nổi bật.</summary>
    public List<ShopProductTab> Tabs { get; set; } = new();
}

public class ShopProductTab
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public IReadOnlyList<Product> Products { get; set; } = Array.Empty<Product>();
}

/// <summary>Dữ liệu trang danh sách / tìm kiếm sản phẩm.</summary>
public class ShopSearchViewModel
{
    public ProductFilter Filter { get; set; } = new();
    public PagedResult<Product> Result { get; set; } = new(Array.Empty<Product>(), 1, 12, 0);
    public List<CategoryListItem> Categories { get; set; } = new();
    public List<BrandListItem> Brands { get; set; } = new();

    public string? CategoryName => Categories.FirstOrDefault(c => c.Id == Filter.CategoryId)?.Name;
    public string? BrandName => Brands.FirstOrDefault(b => b.Id == Filter.BrandId)?.Name;
}

/// <summary>Dữ liệu trang chi tiết sản phẩm.</summary>
public class ShopDetailViewModel
{
    public Product Product { get; set; } = null!;
    public IReadOnlyList<Product> Related { get; set; } = Array.Empty<Product>();
}
