using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface IImportService
{
    Task<PagedResult<Import>> GetPagedAsync(ImportStatus? status, int? supplierId, int page, int pageSize);
    Task<Import?> GetByIdAsync(int id);
    Task<Dictionary<ImportStatus, int>> CountByStatusAsync();

    /// <summary>
    /// Tạo phiếu nhập (trạng thái Chờ duyệt). receiveNow = true: nhận hàng luôn, cộng tồn kho ngay.
    /// </summary>
    Task<(ServiceResult Result, int ImportId)> CreateAsync(int supplierId, string? notes, IReadOnlyList<ImportLine> lines, bool receiveNow);

    /// <summary>Duyệt phiếu (Chờ duyệt -> Đã duyệt).</summary>
    Task<ServiceResult> ApproveAsync(int id);

    /// <summary>Xác nhận đã nhận hàng: cộng số lượng vào tồn kho. Chỉ làm được 1 lần.</summary>
    Task<ServiceResult> ReceiveAsync(int id);

    /// <summary>Hủy phiếu chưa nhận hàng.</summary>
    Task<ServiceResult> CancelAsync(int id);

    /// <summary>Phụ tùng đang kinh doanh có tồn kho thấp (để gợi ý nhập hàng).</summary>
    Task<List<Product>> GetLowStockAsync();
}
