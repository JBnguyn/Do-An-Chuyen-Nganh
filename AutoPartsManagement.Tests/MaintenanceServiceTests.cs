using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Implementations;
using AutoPartsManagement.Services.Models;

namespace AutoPartsManagement.Tests;

public class MaintenanceServiceTests
{
    private const string User = "user-1";

    private static UserVehicle Vios(int km, params MaintenanceRecord[] records)
    {
        var v = new UserVehicle { Id = 1, UserId = User, Make = "Toyota", CarModel = "Vios", Year = 2020, CurrentKm = km };
        foreach (var r in records) v.Records.Add(r);
        return v;
    }

    private static async Task<MaintenanceStatus> StatusOf(UserVehicle v, string key)
    {
        var t = new TestDb();
        var summary = await new MaintenanceService(t.NewContext()).GetSummaryAsync(v);
        return summary.Items.Single(i => i.Item.Key == key);
    }

    [Fact(DisplayName = "Chưa có lịch sử: hẹn mốc thay dầu tròn tiếp theo")]
    public async Task NoRecord_UsesNextRoundMilestone()
    {
        var s = await StatusOf(Vios(12_000), "dau-may");

        Assert.Equal(15_000, s.NextKm);
        Assert.Equal(3_000, s.RemainingKm);
        Assert.Equal(MaintenanceState.Unknown, s.State);
    }

    [Fact(DisplayName = "Còn dưới 500 km tới mốc thay dầu: Sắp đến hạn")]
    public async Task NoRecord_CloseToMilestone_IsDueSoon()
    {
        var s = await StatusOf(Vios(14_600), "dau-may");

        Assert.Equal(400, s.RemainingKm);
        Assert.Equal(MaintenanceState.DueSoon, s.State);
    }

    [Fact(DisplayName = "Đi quá số km kể từ lần thay trước: Quá hạn")]
    public async Task Record_PastInterval_IsOverdue()
    {
        var s = await StatusOf(
            Vios(15_800, new MaintenanceRecord { ItemKey = "dau-may", LastKm = 10_000, LastDate = DateTime.Today.AddMonths(-1) }),
            "dau-may");

        Assert.Equal(15_000, s.NextKm);
        Assert.Equal(-800, s.RemainingKm);
        Assert.Equal(MaintenanceState.Overdue, s.State);
    }

    [Fact(DisplayName = "Vừa thay xong: Còn xa")]
    public async Task Record_JustDone_IsOk()
    {
        var s = await StatusOf(
            Vios(10_200, new MaintenanceRecord { ItemKey = "dau-may", LastKm = 10_000, LastDate = DateTime.Today }),
            "dau-may");

        Assert.Equal(MaintenanceState.Ok, s.State);
    }

    [Fact(DisplayName = "Ắc quy quá 24 tháng: Quá hạn theo thời gian")]
    public async Task Battery_TimeBased_IsOverdue()
    {
        var s = await StatusOf(
            Vios(30_000, new MaintenanceRecord { ItemKey = "ac-quy", LastKm = 0, LastDate = DateTime.Today.AddMonths(-25) }),
            "ac-quy");

        Assert.Equal(MaintenanceState.Overdue, s.State);
    }

    [Fact(DisplayName = "Dầu thay chưa tới km nhưng đã quá 6 tháng: Quá hạn")]
    public async Task Oil_TimeLimitWins_EvenIfKmOk()
    {
        var s = await StatusOf(
            Vios(11_000, new MaintenanceRecord { ItemKey = "dau-may", LastKm = 10_000, LastDate = DateTime.Today.AddMonths(-7) }),
            "dau-may");

        Assert.Equal(MaintenanceState.Overdue, s.State);
    }

    [Fact(DisplayName = "Gợi ý phụ tùng: ưu tiên sản phẩm đúng dòng xe")]
    public async Task Suggestions_PreferMatchingCarModel()
    {
        var s = await StatusOf(Vios(12_000), "dau-may");

        Assert.NotEmpty(s.Suggestions);
        Assert.Equal(TestDb.OilVios, s.Suggestions[0].Id);
        Assert.DoesNotContain(s.Suggestions, p => p.Id == TestDb.Inactive);
    }

    [Fact(DisplayName = "Ghi nhận bảo dưỡng: số km lớn hơn km hiện tại bị từ chối")]
    public async Task MarkDone_KmAboveCurrent_Fails()
    {
        var t = new TestDb();
        var svc = new MaintenanceService(t.NewContext());
        await svc.AddVehicleAsync(User, new UserVehicle { Make = "Toyota", CarModel = "Vios", CurrentKm = 20_000 });
        var vehicle = (await svc.GetVehiclesAsync(User)).Single();

        var result = await new MaintenanceService(t.NewContext()).MarkDoneAsync(vehicle.Id, User, "dau-may", 25_000, null);

        Assert.False(result.Succeeded);
    }

    [Fact(DisplayName = "Ghi nhận bảo dưỡng hợp lệ: lưu lại lịch sử")]
    public async Task MarkDone_Valid_SavesRecord()
    {
        var t = new TestDb();
        var svc = new MaintenanceService(t.NewContext());
        await svc.AddVehicleAsync(User, new UserVehicle { Make = "Toyota", CarModel = "Vios", CurrentKm = 20_000 });
        var vehicle = (await svc.GetVehiclesAsync(User)).Single();

        var result = await new MaintenanceService(t.NewContext()).MarkDoneAsync(vehicle.Id, User, "dau-may", 19_500, null);

        Assert.True(result.Succeeded, result.Message);
        var saved = await new MaintenanceService(t.NewContext()).GetVehicleAsync(vehicle.Id, User);
        var record = Assert.Single(saved!.Records);
        Assert.Equal(19_500, record.LastKm);
    }

    [Fact(DisplayName = "Mỗi tài khoản lưu tối đa 5 xe")]
    public async Task AddVehicle_MaxFivePerUser()
    {
        var t = new TestDb();
        for (var i = 0; i < 5; i++)
            Assert.True((await new MaintenanceService(t.NewContext())
                .AddVehicleAsync(User, new UserVehicle { Make = "Toyota", CarModel = "Xe " + i })).Succeeded);

        var sixth = await new MaintenanceService(t.NewContext())
            .AddVehicleAsync(User, new UserVehicle { Make = "Toyota", CarModel = "Xe 6" });

        Assert.False(sixth.Succeeded);
    }

    [Fact(DisplayName = "Không xem được xe của tài khoản khác")]
    public async Task GetVehicle_OtherUser_ReturnsNull()
    {
        var t = new TestDb();
        var svc = new MaintenanceService(t.NewContext());
        await svc.AddVehicleAsync(User, new UserVehicle { Make = "Toyota", CarModel = "Vios" });
        var vehicle = (await svc.GetVehiclesAsync(User)).Single();

        Assert.Null(await new MaintenanceService(t.NewContext()).GetVehicleAsync(vehicle.Id, "user-khac"));
    }
}
