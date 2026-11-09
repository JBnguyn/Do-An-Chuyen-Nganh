using AutoPartsManagement.Services.Common;

namespace AutoPartsManagement.Services.Models;

/// <summary>
/// Điều kiện tìm kiếm / lọc / sắp xếp / phân trang danh sách phụ tùng.
/// Tên thuộc tính trùng với tên tham số trên query string (?search=...&amp;categoryId=...).
/// </summary>
public class ProductFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }

    /// <summary>"in" = còn hàng, "low" = sắp hết, "out" = hết hàng, null = tất cả.</summary>
    public string? StockStatus { get; set; }

    /// <summary>true = đang kinh doanh, false = ngừng kinh doanh, null = tất cả.</summary>
    public bool? IsActive { get; set; }

    /// <summary>true = chỉ hàng chính hãng xe (OEM), false = chỉ hàng thay thế, null = tất cả.</summary>
    public bool? Genuine { get; set; }

    /// <summary>name | name_desc | price | price_desc | stock | stock_desc | newest</summary>
    public string? SortBy { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = QueryableExtensions.DefaultPageSize;
}

/// <summary>Số liệu tổng quan hiển thị phía trên danh sách phụ tùng.</summary>
public class ProductStats
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStock { get; set; }
    public int OutOfStock { get; set; }
}
