namespace AutoPartsManagement.Web.Helpers;

/// <summary>
/// Hàm hỗ trợ giao diện cửa hàng (icon danh mục, danh sách hãng xe để tra nhanh).
/// </summary>
public static class ShopUi
{
    /// <summary>Hotline hiển thị trên cửa hàng - sửa thành số thật của nhóm.</summary>
    public const string Hotline = "1900 xxxx";

    /// <summary>Các hãng xe dùng cho ô "Tìm theo xe" (tìm theo tên phụ tùng).</summary>
    public static readonly string[] CarMakes = { "Toyota", "Honda", "Mazda", "Hyundai", "Kia", "Ford", "Mitsubishi" };

    /// <summary>Chọn icon Bootstrap Icons theo tên danh mục.</summary>
    public static string CategoryIcon(string? name)
    {
        var n = (name ?? string.Empty).ToLowerInvariant();
        if (n.Contains("phanh")) return "bi-disc";
        if (n.Contains("điện")) return "bi-lightning-charge";
        if (n.Contains("lọc") || n.Contains("dầu") || n.Contains("nhớt")) return "bi-droplet-half";
        if (n.Contains("ly hợp") || n.Contains("truyền")) return "bi-gear-wide";
        if (n.Contains("động cơ")) return "bi-gear-wide-connected";
        if (n.Contains("treo") || n.Contains("gầm") || n.Contains("lái")) return "bi-car-front";
        if (n.Contains("làm mát")) return "bi-thermometer-snow";
        if (n.Contains("đèn") || n.Contains("chiếu sáng")) return "bi-lightbulb";
        if (n.Contains("lốp") || n.Contains("bánh xe") || n.Contains("mâm")) return "bi-record-circle";
        if (n.Contains("kính") || n.Contains("gương") || n.Contains("gạt mưa")) return "bi-window";
        return "bi-box-seam";
    }
}
