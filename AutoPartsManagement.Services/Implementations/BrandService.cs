using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

public class BrandService : IBrandService
{
    private readonly ApplicationDbContext _db;

    public BrandService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<BrandListItem>> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _db.Brands.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(b => b.Name.Contains(s) || (b.Country != null && b.Country.Contains(s)));
        }

        return query
            .OrderBy(b => b.Name)
            .Select(b => new BrandListItem
            {
                Id = b.Id,
                Name = b.Name,
                Country = b.Country,
                LogoUrl = b.LogoUrl,
                ProductCount = b.Products.Count
            })
            .ToPagedResultAsync(page, pageSize);
    }

    public Task<List<Brand>> GetAllAsync() =>
        _db.Brands.AsNoTracking().OrderBy(b => b.Name).ToListAsync();

    public Task<Brand?> GetByIdAsync(int id) =>
        _db.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

    public Task<int> CountProductsAsync(int id) =>
        _db.Products.CountAsync(p => p.BrandId == id);

    public async Task<ServiceResult> CreateAsync(Brand brand)
    {
        brand.Name = brand.Name.Trim();
        brand.Country = brand.Country?.Trim();
        brand.LogoUrl = brand.LogoUrl?.Trim();

        if (await _db.Brands.AnyAsync(b => b.Name == brand.Name))
            return ServiceResult.Failure("Tên hãng đã tồn tại.", nameof(Brand.Name));

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(Brand brand)
    {
        var entity = await _db.Brands.FindAsync(brand.Id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy hãng sản xuất.");

        var name = brand.Name.Trim();
        if (await _db.Brands.AnyAsync(b => b.Name == name && b.Id != brand.Id))
            return ServiceResult.Failure("Tên hãng đã tồn tại.", nameof(Brand.Name));

        entity.Name = name;
        entity.Country = brand.Country?.Trim();
        entity.LogoUrl = brand.LogoUrl?.Trim();
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await _db.Brands.FindAsync(id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy hãng sản xuất.");

        var productCount = await CountProductsAsync(id);
        if (productCount > 0)
            return ServiceResult.Failure(
                $"Không thể xóa: hãng \"{entity.Name}\" đang có {productCount} phụ tùng.");

        _db.Brands.Remove(entity);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
