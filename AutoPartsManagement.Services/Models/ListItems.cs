namespace AutoPartsManagement.Services.Models;

/// <summary>Dòng hiển thị trong danh sách Danh mục (kèm số phụ tùng).</summary>
public class CategoryListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>Dòng hiển thị trong danh sách Hãng sản xuất (kèm số phụ tùng).</summary>
public class BrandListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>Dòng hiển thị trong danh sách Nhà cung cấp (kèm số phiếu nhập).</summary>
public class SupplierListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ImportCount { get; set; }
}
