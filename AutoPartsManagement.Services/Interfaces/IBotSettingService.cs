using AutoPartsManagement.Data.Models;

namespace AutoPartsManagement.Services.Interfaces;

/// <summary>Đọc / lưu cấu hình trợ lý chat (bảng chỉ có 1 dòng).</summary>
public interface IBotSettingService
{
    /// <summary>Lấy cấu hình hiện tại; chưa có thì tạo dòng mặc định.</summary>
    Task<BotSetting> GetAsync();

    /// <summary>Lưu cấu hình Admin vừa sửa.</summary>
    Task SaveAsync(BotSetting input);
}