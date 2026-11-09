using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryListItem>> GetPagedAsync(string? search, int page, int pageSize);
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<int> CountProductsAsync(int id);
    Task<ServiceResult> CreateAsync(Category category);
    Task<ServiceResult> UpdateAsync(Category category);
    Task<ServiceResult> DeleteAsync(int id);
}
