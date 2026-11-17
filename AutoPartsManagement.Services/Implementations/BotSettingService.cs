using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

public class BotSettingService : IBotSettingService
{
    private readonly ApplicationDbContext _db;

    public BotSettingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<BotSetting> GetAsync()
    {
        var setting = await _db.BotSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (setting != null) return setting;

        // Lần đầu: tạo dòng mặc định (giá trị mặc định nằm trong model BotSetting)
        setting = new BotSetting();
        _db.BotSettings.Add(setting);
        await _db.SaveChangesAsync();
        return setting;
    }

    public async Task SaveAsync(BotSetting input)
    {
        var setting = await GetAsync();

        // Chỉ chép các trường Admin được sửa; bỏ khoảng trắng thừa
        setting.AiEnabled = input.AiEnabled;
        setting.BotName = input.BotName.Trim();
        setting.BotPronoun = input.BotPronoun.Trim();
        setting.CustomerPronoun = input.CustomerPronoun.Trim();
        setting.Tone = (input.Tone ?? string.Empty).Trim();
        setting.EmojiLevel = input.EmojiLevel;
        setting.WorkingHours = (input.WorkingHours ?? string.Empty).Trim();
        setting.Policies = string.IsNullOrWhiteSpace(input.Policies) ? null : input.Policies.Trim();
        setting.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }
}