namespace AutoPartsManagement.Services.Models;

/// <summary>Một dòng trong giỏ hàng gửi lên khi đặt hàng.</summary>
public class CartLine
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>Thông tin người nhận khi đặt hàng.</summary>
public class CheckoutInfo
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
}
