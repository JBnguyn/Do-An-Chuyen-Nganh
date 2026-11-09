using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface IProductService
{
    Task<PagedResult<Product>> GetPagedAsync(ProductFilter filter);
    Task<ProductStats> GetStatsAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(Product product);

    /// <summary>
    /// Cập nhật thông tin phụ tùng. Không thay đổi số lượng tồn (Stock) -
    /// tồn kho chỉ thay đổi qua phiếu nhập / phiếu xuất.
    /// </summary>
    Task<ServiceResult> UpdateAsync(Product product);

    /// <summary>
    /// Xóa phụ tùng. Nếu phụ tùng đã có trong phiếu nhập / đơn hàng thì không xóa hẳn
    /// mà chuyển sang "Ngừng kinh doanh" (HardDeleted = false).
    /// </summary>
    Task<(ServiceResult Result, bool HardDeleted)> DeleteAsync(int id);
}
