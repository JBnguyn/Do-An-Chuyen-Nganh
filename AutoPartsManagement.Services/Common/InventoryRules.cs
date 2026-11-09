namespace AutoPartsManagement.Services.Common;

/// <summary>
/// Các quy tắc nghiệp vụ kho dùng chung.
/// </summary>
public static class InventoryRules
{
    /// <summary>Tồn kho từ 1 đến ngưỡng này được coi là "sắp hết hàng".</summary>
    public const int LowStockThreshold = 5;
}
