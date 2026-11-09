using System.ComponentModel.DataAnnotations;
using AutoPartsManagement.Data.Models.Enums;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Phiếu nhập hàng (nhập kho từ nhà cung cấp)
/// </summary>
public class Import
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
    public int SupplierId { get; set; }

    [Required]
    public DateTime ImportDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Tổng tiền là bắt buộc")]
    public decimal TotalAmount { get; set; }

    public ImportStatus Status { get; set; } = ImportStatus.Pending;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
}
