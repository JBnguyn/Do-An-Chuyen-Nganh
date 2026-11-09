using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface IBrandService
{
    Task<PagedResult<BrandListItem>> GetPagedAsync(string? search, int page, int pageSize);
    Task<List<Brand>> GetAllAsync();
    Task<Brand?> GetByIdAsync(int id);
    Task<int> CountProductsAsync(int id);
    Task<ServiceResult> CreateAsync(Brand brand);
    Task<ServiceResult> UpdateAsync(Brand brand);
    Task<ServiceResult> DeleteAsync(int id);
}
