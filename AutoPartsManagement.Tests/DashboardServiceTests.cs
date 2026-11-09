using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Implementations;

namespace AutoPartsManagement.Tests;

public class DashboardServiceTests
{
    private static Order NewOrder(int customerId, decimal total, OrderStatus status, DateTime date) => new()
    {
        CustomerId = customerId,
        TotalAmount = total,
        Status = status,
        OrderDate = date,
        OrderDetails = { new OrderDetail { ProductId = TestDb.OilVios, Quantity = 1, UnitPrice = total } }
    };

    [Fact(DisplayName = "Doanh thu không tính đơn đã hủy")]
    public async Task Revenue_ExcludesCancelledOrders()
    {
        var t = new TestDb();
        using (var db = t.NewContext())
        {
            var c = new Customer { Id = 1, Name = "Khách", Phone = "0900000001" };
            db.Customers.Add(c);
            db.Orders.AddRange(
                NewOrder(1, 1_000_000m, OrderStatus.Delivered, DateTime.Today),
                NewOrder(1, 500_000m, OrderStatus.Pending, DateTime.Today),
                NewOrder(1, 9_999_000m, OrderStatus.Cancelled, DateTime.Today));
            db.SaveChanges();
        }

        var data = await new DashboardService(t.NewContext()).GetAsync(7);

        Assert.Equal(1_500_000m, data.Revenue);
        Assert.Equal(2, data.OrderCount);
        Assert.Equal(1, data.CancelledCount);
        Assert.Equal(1, data.PendingOrders);
        Assert.Equal(750_000m, data.AverageOrderValue);
    }

    [Fact(DisplayName = "So sánh doanh thu với kỳ trước")]
    public async Task Revenue_ComparesWithPreviousPeriod()
    {
        var t = new TestDb();
        using (var db = t.NewContext())
        {
            db.Customers.Add(new Customer { Id = 1, Name = "Khách", Phone = "0900000001" });
            db.Orders.AddRange(
                NewOrder(1, 2_000_000m, OrderStatus.Delivered, DateTime.Today),
                NewOrder(1, 1_000_000m, OrderStatus.Delivered, DateTime.Today.AddDays(-10)));  // thuộc kỳ 7 ngày trước
            db.SaveChanges();
        }

        var data = await new DashboardService(t.NewContext()).GetAsync(7);

        Assert.Equal(2_000_000m, data.Revenue);
        Assert.Equal(1_000_000m, data.PreviousRevenue);
        Assert.Equal(1.0, data.RevenueChange!.Value, 3);   // tăng 100%
        Assert.Equal(7, data.RevenueByDay.Count);
    }

    [Fact(DisplayName = "Giá trị tồn kho = tồn × giá, bỏ qua sản phẩm ngừng bán")]
    public async Task InventoryValue_ActiveProductsOnly()
    {
        var t = new TestDb();
        var data = await new DashboardService(t.NewContext()).GetAsync(30);

        // Vios 10×300k + má phanh 5×500k + dầu chung 20×200k (bỏ sản phẩm ngừng bán)
        Assert.Equal(3_000_000m + 2_500_000m + 4_000_000m, data.InventoryValue);
        Assert.Equal(1, data.LowStockCount);  // má phanh còn 5 (<= ngưỡng 5)
    }
}
