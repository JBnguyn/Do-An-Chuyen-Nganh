using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;

    public CategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<CategoryListItem>> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _db.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.Name.Contains(s) || (c.Description != null && c.Description.Contains(s)));
        }

        return query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItem
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count
            })
            .ToPagedResultAsync(page, pageSize);
    }

    public Task<List<Category>> GetAllAsync() =>
        _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

    public Task<Category?> GetByIdAsync(int id) =>
        _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public Task<int> CountProductsAsync(int id) =>
        _db.Products.CountAsync(p => p.CategoryId == id);

    public async Task<ServiceResult> CreateAsync(Category category)
    {
        category.Name = category.Name.Trim();
        category.Description = category.Description?.Trim();

        if (await _db.Categories.AnyAsync(c => c.Name == category.Name))
            return ServiceResult.Failure("Tên danh mục đã tồn tại.", nameof(Category.Name));

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(Category category)
    {
        var entity = await _db.Categories.FindAsync(category.Id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy danh mục.");

        var name = category.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != category.Id))
            return ServiceResult.Failure("Tên danh mục đã tồn tại.", nameof(Category.Name));

        entity.Name = name;
        entity.Description = category.Description?.Trim();
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity == null)
            return ServiceResult.Failure("Không tìm thấy danh mục.");

        var productCount = await CountProductsAsync(id);
        if (productCount > 0)
            return ServiceResult.Failure(
                $"Không thể xóa: danh mục \"{entity.Name}\" đang có {productCount} phụ tùng. " +
                "Hãy chuyển các phụ tùng sang danh mục khác trước.");

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
