using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Xe của khách hàng ("Xe của tôi") - dùng để tính lịch bảo dưỡng theo số km.
/// </summary>
public class UserVehicle
{
    public int Id { get; set; }

    /// <summary>Id tài khoản đăng nhập (AspNetUsers.Id).</summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hãng xe")]
    [StringLength(50)]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập dòng xe")]
    [StringLength(50, ErrorMessage = "Dòng xe tối đa {1} ký tự")]
    public string CarModel { get; set; } = string.Empty;

    [Range(1980, 2100, ErrorMessage = "Năm sản xuất không hợp lệ")]
    public int? Year { get; set; }

    /// <summary>Số km hiện tại trên đồng hồ (ODO).</summary>
    [Range(0, 2_000_000, ErrorMessage = "Số km không hợp lệ")]
    public int CurrentKm { get; set; }

    /// <summary>Thời điểm cập nhật số km gần nhất.</summary>
    public DateTime KmUpdatedAt { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<MaintenanceRecord> Records { get; set; } = new List<MaintenanceRecord>();

    [NotMapped]
    public string DisplayName => $"{Make} {CarModel}{(Year.HasValue ? " " + Year : "")}";
}
