using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface IMaintenanceService
{
    Task<List<UserVehicle>> GetVehiclesAsync(string userId);
    Task<UserVehicle?> GetVehicleAsync(int id, string userId);
    Task<ServiceResult> AddVehicleAsync(string userId, UserVehicle vehicle);
    Task<ServiceResult> UpdateKmAsync(int id, string userId, int km);
    Task<ServiceResult> DeleteVehicleAsync(int id, string userId);

    /// <summary>Ghi nhận đã làm một hạng mục ở số km / ngày nào (mặc định: km hiện tại, hôm nay).</summary>
    Task<ServiceResult> MarkDoneAsync(int vehicleId, string userId, string itemKey, int? km, DateTime? date);

    /// <summary>Tính tình trạng các hạng mục bảo dưỡng + gợi ý phụ tùng.</summary>
    Task<VehicleMaintenanceSummary> GetSummaryAsync(UserVehicle vehicle);

    /// <summary>Tổng số hạng mục sắp đến hạn / quá hạn trên tất cả xe của tài khoản (cho huy hiệu nhắc).</summary>
    Task<int> CountDueAsync(string userId);
}
