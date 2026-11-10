using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Implementations;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Tests;

public class NotificationTests
{
    private const string User = "user-1";
    private static readonly CheckoutInfo Info = new() { Name = "Nguyễn Văn A", Phone = "0912345678", Address = "12 Lê Lợi, TP.HCM" };

    private static List<CartLine> Cart(params (int id, int qty)[] items) =>
        items.Select(i => new CartLine { ProductId = i.id, Quantity = i.qty }).ToList();

    private static List<Notification> All(TestDb t)
    {
        using var db = t.NewContext();
        return db.Notifications.OrderBy(n => n.Id).ToList();
    }

    [Fact(DisplayName = "Đặt hàng: Admin nhận thông báo đơn mới kèm khách, sản phẩm, tổng tiền")]
    public async Task PlaceOrder_CreatesNewOrderNotification()
    {
        var t = new TestDb();
        var (result, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilGeneric, 2)));

        Assert.True(result.Succeeded);
        var n = Assert.Single(All(t));
        Assert.Equal(NotificationTypes.NewOrder, n.Type);
        Assert.Equal(orderId, n.OrderId);
        Assert.Equal($"/Orders/Detail/{orderId}", n.Link);
        Assert.False(n.IsRead);
        Assert.Contains("Nguyễn Văn A", n.Message);
        Assert.Contains("Dầu động cơ 5W-30 4L ×2", n.Message);
        Assert.Contains("400.000", n.Message);
    }

    [Fact(DisplayName = "Tồn kho vừa xuống mức sắp hết: có thông báo Sắp hết hàng")]
    public async Task PlaceOrder_CrossingLowStock_CreatesLowStockNotification()
    {
        var t = new TestDb();
        await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 6)));  // 10 -> 4

        var low = Assert.Single(All(t), n => n.Type == NotificationTypes.LowStock);
        Assert.Equal(TestDb.OilVios, low.ProductId);
        Assert.Contains("còn 4", low.Message);
        Assert.Equal($"/Imports/Create?productId={TestDb.OilVios}", low.Link);
    }

    [Fact(DisplayName = "Bán hết hàng: có thông báo Hết hàng")]
    public async Task PlaceOrder_SellingOut_CreatesOutOfStockNotification()
    {
        var t = new TestDb();
        await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 10)));

        Assert.Single(All(t), n => n.Type == NotificationTypes.OutOfStock && n.ProductId == TestDb.OilVios);
        Assert.DoesNotContain(All(t), n => n.Type == NotificationTypes.LowStock);
    }

    [Fact(DisplayName = "Sản phẩm vốn đã sắp hết: không báo lặp lại mỗi đơn")]
    public async Task PlaceOrder_AlreadyLowStock_NoDuplicateWarning()
    {
        var t = new TestDb();
        await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.BrakePad, 1)));  // 5 -> 4

        Assert.Single(All(t));   // chỉ có thông báo đơn mới
    }

    [Fact(DisplayName = "Đặt hàng thất bại: không tạo thông báo")]
    public async Task PlaceOrder_Failed_NoNotification()
    {
        var t = new TestDb();
        await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.BrakePad, 99)));

        Assert.Empty(All(t));
    }

    [Fact(DisplayName = "Khách hủy đơn: Admin nhận thông báo hủy")]
    public async Task CancelByUser_CreatesCancelNotification()
    {
        var t = new TestDb();
        var (_, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilGeneric, 1)));
        await new OrderService(t.NewContext()).CancelByUserAsync(orderId, User);

        var cancel = Assert.Single(All(t), n => n.Type == NotificationTypes.OrderCancelled);
        Assert.Equal(orderId, cancel.OrderId);
    }

    [Fact(DisplayName = "Đếm chưa đọc, đánh dấu đã đọc")]
    public async Task MarkRead_UpdatesUnreadCount()
    {
        var t = new TestDb();
        await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilGeneric, 1)));
        await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilGeneric, 1)));

        Assert.Equal(2, await new NotificationService(t.NewContext()).CountUnreadAsync());

        var first = All(t)[0];
        var opened = await new NotificationService(t.NewContext()).MarkReadAsync(first.Id);
        Assert.True(opened!.IsRead);
        Assert.Equal(1, await new NotificationService(t.NewContext()).CountUnreadAsync());

        Assert.Equal(1, await new NotificationService(t.NewContext()).MarkAllReadAsync());
        Assert.Equal(0, await new NotificationService(t.NewContext()).CountUnreadAsync());
    }
}
