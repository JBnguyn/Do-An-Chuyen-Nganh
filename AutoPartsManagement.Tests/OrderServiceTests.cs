using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Implementations;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Tests;

public class OrderServiceTests
{
    private const string User = "user-1";
    private static readonly CheckoutInfo Info = new() { Name = "Nguyễn Văn A", Phone = "0912345678", Address = "12 Lê Lợi, TP.HCM" };

    private static List<CartLine> Cart(params (int id, int qty)[] items) =>
        items.Select(i => new CartLine { ProductId = i.id, Quantity = i.qty }).ToList();

    [Fact(DisplayName = "Đặt hàng: trừ tồn kho và tính tiền theo giá trong DB")]
    public async Task PlaceOrder_DeductsStock_AndUsesDbPrice()
    {
        var t = new TestDb();
        using var db = t.NewContext();
        var svc = new OrderService(db);

        var (result, orderId) = await svc.PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 2), (TestDb.BrakePad, 1)));

        Assert.True(result.Succeeded, result.Message);
        Assert.True(orderId > 0);
        Assert.Equal(8, t.StockOf(TestDb.OilVios));
        Assert.Equal(4, t.StockOf(TestDb.BrakePad));

        var order = await new OrderService(t.NewContext()).GetByIdAsync(orderId);
        Assert.NotNull(order);
        Assert.Equal(2 * 300_000m + 500_000m, order!.TotalAmount);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(User, order.Customer!.UserId);
    }

    [Fact(DisplayName = "Đặt hàng: gộp các dòng trùng sản phẩm")]
    public async Task PlaceOrder_MergesDuplicateLines()
    {
        var t = new TestDb();
        var svc = new OrderService(t.NewContext());

        var (result, orderId) = await svc.PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 1), (TestDb.OilVios, 2)));

        Assert.True(result.Succeeded);
        var order = await new OrderService(t.NewContext()).GetByIdAsync(orderId);
        var line = Assert.Single(order!.OrderDetails);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(7, t.StockOf(TestDb.OilVios));
    }

    [Fact(DisplayName = "Đặt hàng: từ chối khi mua nhiều hơn tồn kho, kho không bị trừ")]
    public async Task PlaceOrder_RejectsWhenNotEnoughStock()
    {
        var t = new TestDb();
        var svc = new OrderService(t.NewContext());

        var (result, orderId) = await svc.PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 1), (TestDb.BrakePad, 6)));

        Assert.False(result.Succeeded);
        Assert.Equal(0, orderId);
        Assert.Contains("chỉ còn 5", result.Message);
        Assert.Equal(10, t.StockOf(TestDb.OilVios));
        Assert.Equal(5, t.StockOf(TestDb.BrakePad));
    }

    [Fact(DisplayName = "Đặt hàng: từ chối sản phẩm ngừng kinh doanh")]
    public async Task PlaceOrder_RejectsInactiveProduct()
    {
        var t = new TestDb();
        var (result, _) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.Inactive, 1)));

        Assert.False(result.Succeeded);
        Assert.Equal(50, t.StockOf(TestDb.Inactive));
    }

    [Fact(DisplayName = "Đặt hàng: từ chối giỏ hàng trống")]
    public async Task PlaceOrder_RejectsEmptyCart()
    {
        var t = new TestDb();
        var (result, _) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 0)));

        Assert.False(result.Succeeded);
    }

    [Fact(DisplayName = "Khách hủy đơn Chờ xác nhận: hoàn lại tồn kho")]
    public async Task CancelByUser_Pending_RestoresStock()
    {
        var t = new TestDb();
        var (_, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 4)));
        Assert.Equal(6, t.StockOf(TestDb.OilVios));

        var result = await new OrderService(t.NewContext()).CancelByUserAsync(orderId, User);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(10, t.StockOf(TestDb.OilVios));
    }

    [Fact(DisplayName = "Khách không thể hủy đơn của người khác")]
    public async Task CancelByUser_OtherUser_Fails()
    {
        var t = new TestDb();
        var (_, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 4)));

        var result = await new OrderService(t.NewContext()).CancelByUserAsync(orderId, "user-khac");

        Assert.False(result.Succeeded);
        Assert.Equal(6, t.StockOf(TestDb.OilVios));
    }

    [Fact(DisplayName = "Khách không thể hủy đơn đã được xác nhận")]
    public async Task CancelByUser_NotPending_Fails()
    {
        var t = new TestDb();
        var (_, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.OilVios, 4)));
        await new OrderService(t.NewContext()).UpdateStatusAsync(orderId, OrderStatus.Processing);

        var result = await new OrderService(t.NewContext()).CancelByUserAsync(orderId, User);

        Assert.False(result.Succeeded);
        Assert.Equal(6, t.StockOf(TestDb.OilVios));
    }

    [Fact(DisplayName = "Admin hủy đơn đang xử lý: hoàn lại tồn kho")]
    public async Task UpdateStatus_Cancel_RestoresStock()
    {
        var t = new TestDb();
        var (_, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.BrakePad, 3)));
        await new OrderService(t.NewContext()).UpdateStatusAsync(orderId, OrderStatus.Processing);

        var result = await new OrderService(t.NewContext()).UpdateStatusAsync(orderId, OrderStatus.Cancelled);

        Assert.True(result.Succeeded);
        Assert.Equal(5, t.StockOf(TestDb.BrakePad));
    }

    [Fact(DisplayName = "Đơn đã giao xong thì bị khóa, không hủy được")]
    public async Task UpdateStatus_Delivered_IsLocked()
    {
        var t = new TestDb();
        var (_, orderId) = await new OrderService(t.NewContext()).PlaceOrderAsync(User, Info, Cart((TestDb.BrakePad, 3)));
        await new OrderService(t.NewContext()).UpdateStatusAsync(orderId, OrderStatus.Delivered);

        var result = await new OrderService(t.NewContext()).UpdateStatusAsync(orderId, OrderStatus.Cancelled);

        Assert.False(result.Succeeded);
        Assert.Equal(2, t.StockOf(TestDb.BrakePad));
    }
}
