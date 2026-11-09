using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

/// <summary>
/// Thống kê cho Admin: doanh thu, đơn hàng, sản phẩm bán chạy, tồn kho.
/// Quy ước: doanh thu = tổng tiền đơn KHÔNG bị hủy, tính theo ngày đặt hàng.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardData> GetAsync(int days)
    {
        days = Math.Clamp(days, 1, 365);
        var today = DateTime.Today;
        var from = today.AddDays(-(days - 1));
        var to = today.AddDays(1);                 // hết ngày hôm nay
        var prevFrom = from.AddDays(-days);

        var data = new DashboardData { Days = days, From = from, To = today };

        // ---- Đơn hàng trong kỳ (kèm chi tiết để thống kê sản phẩm / danh mục) ----
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product).ThenInclude(p => p!.Category)
            .Where(o => o.OrderDate >= from && o.OrderDate < to)
            .ToListAsync();

        var valid = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        data.Revenue = valid.Sum(o => o.TotalAmount);
        data.OrderCount = valid.Count;
        data.CancelledCount = orders.Count - valid.Count;

        data.PreviousRevenue = await _db.Orders
            .Where(o => o.OrderDate >= prevFrom && o.OrderDate < from && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        // Doanh thu từng ngày (có cả ngày không bán được để đường biểu đồ liền mạch)
        var byDay = valid.GroupBy(o => o.OrderDate.Date)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(o => o.TotalAmount), Orders: g.Count()));
        for (var d = from; d < to; d = d.AddDays(1))
        {
            var v = byDay.TryGetValue(d, out var x) ? x : (0m, 0);
            data.RevenueByDay.Add(new DailyRevenue(d, v.Item1, v.Item2));
        }

        var lines = valid.SelectMany(o => o.OrderDetails).ToList();

        data.RevenueByCategory = lines
            .GroupBy(l => l.Product?.Category?.Name ?? "Khác")
            .Select(g => new NamedValue(g.Key, g.Sum(l => l.Quantity * l.UnitPrice)))
            .OrderByDescending(x => x.Value)
            .ToList();

        data.TopProducts = lines
            .GroupBy(l => l.ProductId)
            .Select(g => new TopProduct(
                g.Key,
                g.First().Product?.Name ?? $"#{g.Key}",
                g.First().Product?.PartNumber ?? "",
                g.Sum(l => l.Quantity),
                g.Sum(l => l.Quantity * l.UnitPrice)))
            .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        data.StatusCounts = Enum.GetValues<OrderStatus>()
            .ToDictionary(s => s, s => orders.Count(o => o.Status == s));

        // ---- Nhập kho trong kỳ ----
        data.ImportCost = await _db.Imports
            .Where(i => i.Status == ImportStatus.Received && i.ImportDate >= from && i.ImportDate < to)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

        // ---- Chỉ số hiện tại ----
        const int low = InventoryRules.LowStockThreshold;
        data.PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        data.InventoryValue = await _db.Products.Where(p => p.IsActive).SumAsync(p => (decimal?)(p.Stock * p.Price)) ?? 0;
        data.LowStockCount = await _db.Products.CountAsync(p => p.IsActive && p.Stock <= low);
        data.CustomerCount = await _db.Customers.CountAsync();

        data.LowStockProducts = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= low)
            .OrderBy(p => p.Stock).ThenBy(p => p.Name)
            .Take(8)
            .ToListAsync();

        data.RecentOrders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(6)
            .ToListAsync();

        return data;
    }
}
