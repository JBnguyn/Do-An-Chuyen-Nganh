using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Services.Interfaces;

public interface IDashboardService
{
    /// <summary>Tính số liệu Dashboard cho <paramref name="days"/> ngày gần nhất (tính cả hôm nay).</summary>
    Task<DashboardData> GetAsync(int days);
}
