using AutoPartsManagement.Data.Models.Enums;
using AutoPartsManagement.Services.Implementations;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Tests;

public class ImportServiceTests
{
    private static List<ImportLine> Lines(params (int id, int qty, decimal price)[] items) =>
        items.Select(i => new ImportLine { ProductId = i.id, Quantity = i.qty, UnitPrice = i.price }).ToList();

    [Fact(DisplayName = "Tạo phiếu chờ duyệt: chưa cộng tồn kho")]
    public async Task Create_Pending_DoesNotChangeStock()
    {
        var t = new TestDb();
        var (result, id) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, null, Lines((TestDb.BrakePad, 10, 350_000m)), receiveNow: false);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(5, t.StockOf(TestDb.BrakePad));

        var import = await new ImportService(t.NewContext()).GetByIdAsync(id);
        Assert.Equal(ImportStatus.Pending, import!.Status);
        Assert.Equal(3_500_000m, import.TotalAmount);
    }

    [Fact(DisplayName = "Tạo phiếu và nhận hàng ngay: cộng tồn kho")]
    public async Task Create_ReceiveNow_AddsStock()
    {
        var t = new TestDb();
        var (result, id) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, "Nhập gấp", Lines((TestDb.BrakePad, 10, 350_000m), (TestDb.OilVios, 5, 220_000m)), receiveNow: true);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(15, t.StockOf(TestDb.BrakePad));
        Assert.Equal(15, t.StockOf(TestDb.OilVios));
        Assert.Equal(ImportStatus.Received, (await new ImportService(t.NewContext()).GetByIdAsync(id))!.Status);
    }

    [Fact(DisplayName = "Quy trình duyệt -> nhận hàng: chỉ cộng kho đúng 1 lần")]
    public async Task Approve_Then_Receive_AddsStockOnce()
    {
        var t = new TestDb();
        var (_, id) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, null, Lines((TestDb.BrakePad, 10, 350_000m)), receiveNow: false);

        Assert.True((await new ImportService(t.NewContext()).ApproveAsync(id)).Succeeded);
        Assert.True((await new ImportService(t.NewContext()).ReceiveAsync(id)).Succeeded);
        Assert.Equal(15, t.StockOf(TestDb.BrakePad));

        // Nhận lần 2 phải bị từ chối, tồn kho giữ nguyên
        Assert.False((await new ImportService(t.NewContext()).ReceiveAsync(id)).Succeeded);
        Assert.Equal(15, t.StockOf(TestDb.BrakePad));
    }

    [Fact(DisplayName = "Không thể hủy phiếu đã nhận hàng")]
    public async Task Cancel_AfterReceive_Fails()
    {
        var t = new TestDb();
        var (_, id) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, null, Lines((TestDb.BrakePad, 10, 350_000m)), receiveNow: true);

        var result = await new ImportService(t.NewContext()).CancelAsync(id);

        Assert.False(result.Succeeded);
        Assert.Equal(15, t.StockOf(TestDb.BrakePad));
    }

    [Fact(DisplayName = "Phiếu đã hủy thì không nhận hàng được")]
    public async Task Receive_AfterCancel_Fails()
    {
        var t = new TestDb();
        var (_, id) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, null, Lines((TestDb.BrakePad, 10, 350_000m)), receiveNow: false);
        await new ImportService(t.NewContext()).CancelAsync(id);

        Assert.False((await new ImportService(t.NewContext()).ReceiveAsync(id)).Succeeded);
        Assert.Equal(5, t.StockOf(TestDb.BrakePad));
    }

    [Theory(DisplayName = "Từ chối số lượng không hợp lệ")]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(10_001)]
    public async Task Create_RejectsInvalidQuantity(int qty)
    {
        var t = new TestDb();
        var (result, _) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, null, Lines((TestDb.BrakePad, qty, 350_000m)), receiveNow: true);

        Assert.False(result.Succeeded);
        Assert.Equal(5, t.StockOf(TestDb.BrakePad));
    }

    [Fact(DisplayName = "Từ chối phiếu có sản phẩm bị trùng dòng")]
    public async Task Create_RejectsDuplicateProduct()
    {
        var t = new TestDb();
        var (result, _) = await new ImportService(t.NewContext())
            .CreateAsync(TestDb.SupplierId, null, Lines((TestDb.BrakePad, 1, 1m), (TestDb.BrakePad, 2, 1m)), receiveNow: true);

        Assert.False(result.Succeeded);
    }

    [Fact(DisplayName = "Từ chối nhà cung cấp không tồn tại")]
    public async Task Create_RejectsUnknownSupplier()
    {
        var t = new TestDb();
        var (result, _) = await new ImportService(t.NewContext())
            .CreateAsync(999, null, Lines((TestDb.BrakePad, 1, 1m)), receiveNow: true);

        Assert.False(result.Succeeded);
        Assert.Equal("SupplierId", result.Field);
    }
}
