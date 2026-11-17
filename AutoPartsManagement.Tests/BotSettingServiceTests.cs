using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Implementations;

namespace AutoPartsManagement.Tests;

public class BotSettingServiceTests
{
    [Fact(DisplayName = "Cấu hình bot: lần đầu tự tạo dòng mặc định")]
    public async Task Get_CreatesDefaultRow()
    {
        var t = new TestDb();

        var s = await new BotSettingService(t.NewContext()).GetAsync();

        Assert.Equal("mình", s.BotPronoun);
        Assert.Equal("bạn", s.CustomerPronoun);
        using var db = t.NewContext();
        Assert.Single(db.BotSettings);
    }

    [Fact(DisplayName = "Cấu hình bot: gọi nhiều lần vẫn chỉ có 1 dòng")]
    public async Task Get_Twice_KeepsOneRow()
    {
        var t = new TestDb();

        await new BotSettingService(t.NewContext()).GetAsync();
        await new BotSettingService(t.NewContext()).GetAsync();

        using var db = t.NewContext();
        Assert.Single(db.BotSettings);
    }

    [Fact(DisplayName = "Cấu hình bot: lưu thì cập nhật và bỏ khoảng trắng thừa")]
    public async Task Save_UpdatesAndTrims()
    {
        var t = new TestDb();

        await new BotSettingService(t.NewContext()).SaveAsync(new BotSetting
        {
            AiEnabled = false,
            BotName = "  Bé Ba  ",
            BotPronoun = "em",
            CustomerPronoun = "anh/chị",
            Tone = "lễ phép",
            EmojiLevel = EmojiLevel.None,
            WorkingHours = "7:30 - 18:00",
            Policies = "   "
        });

        var s = await new BotSettingService(t.NewContext()).GetAsync();
        Assert.False(s.AiEnabled);
        Assert.Equal("Bé Ba", s.BotName);
        Assert.Equal("em", s.BotPronoun);
        Assert.Equal(EmojiLevel.None, s.EmojiLevel);
        Assert.Null(s.Policies);
    }
}