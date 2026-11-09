namespace AutoPartsManagement.Services.Common;

/// <summary>Một hạng mục bảo dưỡng định kỳ.</summary>
public record MaintenanceItem(
    string Key,
    string Name,
    int? IntervalKm,
    int? IntervalMonths,
    string Icon,
    string[] Keywords,
    string Note);

/// <summary>
/// Lịch bảo dưỡng tham khảo cho xe du lịch phổ thông.
/// Đây là mức phổ biến, khách nên đối chiếu thêm sổ bảo dưỡng của xe.
/// </summary>
public static class MaintenanceSchedule
{
    public static readonly IReadOnlyList<MaintenanceItem> Items = new List<MaintenanceItem>
    {
        new("dau-may", "Thay dầu động cơ", 5000, 6, "bi-droplet-fill",
            new[] { "dầu động cơ", "dầu máy", "nhớt" }, "Dầu bôi trơn và làm mát động cơ."),
        new("loc-dau", "Thay lọc dầu", 10000, 12, "bi-funnel-fill",
            new[] { "lọc dầu", "lọc nhớt" }, "Thường thay cùng lúc với dầu động cơ (cách 1 lần thay dầu)."),
        new("loc-gio", "Thay lọc gió động cơ", 20000, 24, "bi-wind",
            new[] { "lọc gió" }, "Đi đường bụi nhiều nên thay sớm hơn."),
        new("ma-phanh", "Kiểm tra / thay má phanh", 30000, null, "bi-disc",
            new[] { "má phanh", "bố thắng" }, "Thay khi má phanh mòn hoặc phanh kêu."),
        new("bugi", "Thay bugi", 40000, null, "bi-lightning-charge-fill",
            new[] { "bugi" }, "Bugi Iridium có tuổi thọ cao hơn bugi thường."),
        new("nuoc-lam-mat", "Thay nước làm mát", 40000, 24, "bi-thermometer-half",
            new[] { "nước làm mát" }, "Giúp động cơ không quá nhiệt."),
        new("dau-hop-so", "Thay dầu hộp số", 40000, 48, "bi-gear-fill",
            new[] { "dầu hộp số" }, "Xe số tự động nên kiểm tra định kỳ."),
        new("giam-xoc", "Kiểm tra giảm xóc (phuộc nhún)", 80000, null, "bi-arrows-collapse",
            new[] { "phuộc", "giảm xóc", "giảm chấn" }, "Thay khi xe bị nhún nhiều, chảy dầu phuộc."),
        new("ac-quy", "Kiểm tra / thay ắc quy", null, 24, "bi-battery-half",
            new[] { "ắc quy" }, "Tuổi thọ ắc quy thường 2-3 năm."),
        new("lop", "Kiểm tra / thay lốp", 50000, null, "bi-record-circle",
            new[] { "lốp" }, "Kiểm tra độ mòn gai lốp và áp suất hằng tháng; đảo lốp mỗi 10.000 km."),
        new("gat-mua", "Thay gạt mưa", null, 12, "bi-cloud-drizzle",
            new[] { "gạt mưa" }, "Thay khi gạt không sạch, để lại vệt hoặc kêu.")
    };

    public static MaintenanceItem? Find(string key) => Items.FirstOrDefault(i => i.Key == key);
}
