using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Lần bảo dưỡng gần nhất của một hạng mục (vd: thay dầu máy) trên một xe.
/// Mỗi xe có tối đa 1 bản ghi cho mỗi hạng mục.
/// </summary>
public class MaintenanceRecord
{
    public int Id { get; set; }

    public int UserVehicleId { get; set; }

    /// <summary>Mã hạng mục bảo dưỡng (vd: "dau-may", "loc-gio") - xem MaintenanceSchedule.</summary>
    [Required]
    [StringLength(50)]
    public string ItemKey { get; set; } = string.Empty;

    /// <summary>Số km lúc thực hiện.</summary>
    public int LastKm { get; set; }

    /// <summary>Ngày thực hiện.</summary>
    public DateTime? LastDate { get; set; }

    public virtual UserVehicle? UserVehicle { get; set; }
}
