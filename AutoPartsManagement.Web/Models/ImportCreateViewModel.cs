using System.ComponentModel.DataAnnotations;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Web.Models;

/// <summary>Form tạo phiếu nhập kho.</summary>
public class ImportCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
    [Display(Name = "Nhà cung cấp")]
    public int SupplierId { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú tối đa {1} ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    /// <summary>Nhận hàng ngay: cộng tồn kho luôn khi lưu phiếu.</summary>
    public bool ReceiveNow { get; set; }

    public List<ImportLine> Lines { get; set; } = new();

    // --- Dữ liệu cho form ---
    public List<Supplier> Suppliers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
