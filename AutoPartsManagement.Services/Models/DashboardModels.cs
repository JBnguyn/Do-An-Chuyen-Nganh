using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;

namespace AutoPartsManagement.Services.Models;

/// <summary>Số liệu tổng quan cho trang Dashboard của Admin.</summary>
public class DashboardData
{
    public int Days { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    // --- Chỉ số chính (trong kỳ) ---
    /// <summary>Doanh thu: tổng tiền các đơn không bị hủy.</summary>
    public decimal Revenue { get; set; }
    /// <summary>Doanh thu kỳ trước cùng độ dài (để so sánh).</summary>
    public decimal PreviousRevenue { get; set; }
    public int OrderCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal AverageOrderValue => OrderCount == 0 ? 0 : Revenue / OrderCount;
    public double CancelRate => OrderCount + CancelledCount == 0 ? 0 : (double)CancelledCount / (OrderCount + CancelledCount);
    /// <summary>Tiền nhập hàng của các phiếu đã nhận trong kỳ.</summary>
    public decimal ImportCost { get; set; }

    // --- Chỉ số hiện tại (không theo kỳ) ---
    public int PendingOrders { get; set; }
    public decimal InventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int CustomerCount { get; set; }

    /// <summary>% thay đổi doanh thu so với kỳ trước (null nếu kỳ trước = 0).</summary>
    public double? RevenueChange => PreviousRevenue == 0 ? null : (double)((Revenue - PreviousRevenue) / PreviousRevenue);

    // --- Dữ liệu biểu đồ ---
    public List<DailyRevenue> RevenueByDay { get; set; } = new();
    public List<NamedValue> RevenueByCategory { get; set; } = new();
    public List<TopProduct> TopProducts { get; set; } = new();
    public Dictionary<OrderStatus, int> StatusCounts { get; set; } = new();

    // --- Danh sách ---
    public List<Product> LowStockProducts { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
}

public record DailyRevenue(DateTime Date, decimal Revenue, int Orders);
public record NamedValue(string Name, decimal Value);
public record TopProduct(int ProductId, string Name, string PartNumber, int Quantity, decimal Revenue);
