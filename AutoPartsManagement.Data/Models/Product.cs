using System.ComponentModel.DataAnnotations;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Phụ tùng ô tô (sản phẩm)
/// </summary>
public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã phụ tùng là bắt buộc")]
    [StringLength(50)]
    public string PartNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hãng sản xuất")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Giá bán là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
    public int Stock { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>
    /// Thời gian bảo hành (tháng)
    /// </summary>
    public int? WarrantyMonths { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual Brand? Brand { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
}
