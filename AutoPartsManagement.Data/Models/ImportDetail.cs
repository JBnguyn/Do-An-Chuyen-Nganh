using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Chi tiết phiếu nhập (mỗi dòng sản phẩm trong phiếu nhập)
/// </summary>
public class ImportDetail
{
    public int Id { get; set; }

    [Required]
    public int ImportId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Số lượng là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Đơn giá là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal SubTotal => Quantity * UnitPrice;

    // Navigation properties
    public virtual Import? Import { get; set; }
    public virtual Product? Product { get; set; }
}
