using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<SupplierListItem>> GetPagedAsync(string? search, int page, int pageSize);
    Task<List<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<int> CountImportsAsync(int id);
    Task<ServiceResult> CreateAsync(Supplier supplier);
    Task<ServiceResult> UpdateAsync(Supplier supplier);
    Task<ServiceResult> DeleteAsync(int id);
}
