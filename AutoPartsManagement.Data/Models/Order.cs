using System.ComponentModel.DataAnnotations;
using AutoPartsManagement.Data.Models.Enums;

namespace AutoPartsManagement.Data.Models;

/// <summary>
/// Đơn hàng bán
/// </summary>
public class Order
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
    public int CustomerId { get; set; }

    [Required]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Tổng tiền là bắt buộc")]
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
