namespace AutoPartsManagement.Services.Models;

/// <summary>Một dòng sản phẩm khi tạo phiếu nhập.</summary>
public class ImportLine
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
