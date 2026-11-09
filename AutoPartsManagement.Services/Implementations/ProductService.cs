using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;

    public ProductService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<Product>> GetPagedAsync(ProductFilter filter)
    {
        IQueryable<Product> query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand);

        // --- Tìm kiếm theo tên hoặc mã phụ tùng ---
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.PartNumber.Contains(s));
        }

        // --- Lọc ---
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (filter.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filter.BrandId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(p => p.IsActive == filter.IsActive.Value);

        const int low = InventoryRules.LowStockThreshold;
        query = filter.StockStatus switch
        {
            "out" => query.Where(p => p.Stock == 0),
            "low" => query.Where(p => p.Stock > 0 && p.Stock <= low),
            "in" => query.Where(p => p.Stock > low),
            _ => query
        };

        // --- Sắp xếp ---
        var ordered = filter.SortBy switch
        {
            "name_desc" => query.OrderByDescending(p => p.Name),
            "price" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "stock" => query.OrderBy(p => p.Stock),
            "stock_desc" => query.OrderByDescending(p => p.Stock),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };

        return ordered.ThenBy(p => p.Id).ToPagedResultAsync(filter.Page, filter.PageSize);
    }

    public async Task<ProductStats> GetStatsAsync()
    {
        const int low = InventoryRules.LowStockThreshold;
        var active = _db.Products.Where(p => p.IsActive);

        return new ProductStats
        {
            TotalProducts = await _db.Products.CountAsync(),
            ActiveProducts = await active.CountAsync(),
            LowStock = await active.CountAsync(p => p.Stock > 0 && p.Stock <= low),
            OutOfStock = await active.CountAsync(p => p.Stock == 0)
        };
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<ServiceResult> CreateAsync(Product product)
    {
        Normalize(product);

        var check = await ValidateAsync(product);
        if (!check.Succeeded) return check;

        product.Id = 0;
        product.CreatedAt = DateTime.Now;
        product.UpdatedAt = null;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(Product product)
    {
        var entity = await _db.Products.FindAsync(product.Id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy phụ tùng.");

        Normalize(product);

        var check = await ValidateAsync(product);
        if (!check.Succeeded) return check;

        entity.Name = product.Name;
        entity.PartNumber = product.PartNumber;
        entity.CategoryId = product.CategoryId;
        entity.BrandId = product.BrandId;
        entity.Price = product.Price;
        entity.Description = product.Description;
        entity.ImageUrl = product.ImageUrl;
        entity.WarrantyMonths = product.WarrantyMonths;
        entity.IsActive = product.IsActive;
        entity.UpdatedAt = DateTime.Now;
        // Không cập nhật entity.Stock: tồn kho chỉ thay đổi qua nhập / xuất kho.

        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<(ServiceResult Result, bool HardDeleted)> DeleteAsync(int id)
    {
        var entity = await _db.Products.FindAsync(id);
        if (entity == null)
            return (ServiceResult.Failure("Không tìm thấy phụ tùng."), false);

        var hasTransactions =
            await _db.ImportDetails.AnyAsync(d => d.ProductId == id) ||
            await _db.OrderDetails.AnyAsync(d => d.ProductId == id);

        if (hasTransactions)
        {
            // Đã phát sinh giao dịch -> giữ lại dữ liệu lịch sử, chỉ ngừng kinh doanh
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return (ServiceResult.Success(
                $"Phụ tùng \"{entity.Name}\" đã có trong phiếu nhập / đơn hàng nên không xóa hẳn, " +
                "đã chuyển sang trạng thái Ngừng kinh doanh."), false);
        }

        _db.Products.Remove(entity);
        await _db.SaveChangesAsync();
        return (ServiceResult.Success($"Đã xóa phụ tùng \"{entity.Name}\"."), true);
    }

    private async Task<ServiceResult> ValidateAsync(Product product)
    {
        if (await _db.Products.AnyAsync(p => p.PartNumber == product.PartNumber && p.Id != product.Id))
            return ServiceResult.Failure("Mã phụ tùng đã tồn tại.", nameof(Product.PartNumber));

        if (!await _db.Categories.AnyAsync(c => c.Id == product.CategoryId))
            return ServiceResult.Failure("Danh mục không hợp lệ.", nameof(Product.CategoryId));

        if (!await _db.Brands.AnyAsync(b => b.Id == product.BrandId))
            return ServiceResult.Failure("Hãng sản xuất không hợp lệ.", nameof(Product.BrandId));

        return ServiceResult.Success();
    }

    private static void Normalize(Product p)
    {
        p.Name = p.Name.Trim();
        p.PartNumber = p.PartNumber.Trim().ToUpperInvariant();
        p.Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim();
        p.ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? null : p.ImageUrl.Trim();
    }
}
