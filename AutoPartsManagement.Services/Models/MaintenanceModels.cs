using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;

namespace AutoPartsManagement.Services.Models;

public enum MaintenanceState
{
    Ok = 0,         // Còn xa
    Unknown = 1,    // Chưa ghi nhận lần thay gần nhất
    DueSoon = 2,    // Sắp đến hạn
    Overdue = 3     // Đã quá hạn
}

/// <summary>Tình trạng một hạng mục bảo dưỡng của một xe.</summary>
public class MaintenanceStatus
{
    public MaintenanceItem Item { get; set; } = null!;
    public bool HasRecord { get; set; }
    public int? LastKm { get; set; }
    public DateTime? LastDate { get; set; }
    public int? NextKm { get; set; }
    public DateTime? NextDate { get; set; }

    /// <summary>Số km còn lại đến mốc tiếp theo (âm = đã quá).</summary>
    public int? RemainingKm { get; set; }

    public MaintenanceState State { get; set; }

    /// <summary>Phụ tùng gợi ý trong kho cho hạng mục này.</summary>
    public List<Product> Suggestions { get; set; } = new();

    public bool NeedsAttention => State is MaintenanceState.DueSoon or MaintenanceState.Overdue;
}

/// <summary>Tổng hợp lịch bảo dưỡng của một xe.</summary>
public class VehicleMaintenanceSummary
{
    public UserVehicle Vehicle { get; set; } = null!;
    public List<MaintenanceStatus> Items { get; set; } = new();
    public int DueCount => Items.Count(i => i.NeedsAttention);
}
