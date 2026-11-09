using System.Globalization;
using System.Text;
using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using AutoPartsManagement.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

/// <summary>
/// Xe của tôi + nhắc lịch bảo dưỡng theo số km / thời gian.
/// </summary>
public class MaintenanceService : IMaintenanceService
{
    private const int MaxVehiclesPerUser = 5;
    private const int SuggestionsPerItem = 3;

    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ApplicationDbContext _db;

    public MaintenanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ===== Xe =====

    public Task<List<UserVehicle>> GetVehiclesAsync(string userId) =>
        _db.UserVehicles
            .AsNoTracking()
            .Include(v => v.Records)
            .Where(v => v.UserId == userId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();

    public Task<UserVehicle?> GetVehicleAsync(int id, string userId) =>
        _db.UserVehicles
            .AsNoTracking()
            .Include(v => v.Records)
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

    public async Task<ServiceResult> AddVehicleAsync(string userId, UserVehicle vehicle)
    {
        if (await _db.UserVehicles.CountAsync(v => v.UserId == userId) >= MaxVehiclesPerUser)
            return ServiceResult.Failure($"Mỗi tài khoản lưu tối đa {MaxVehiclesPerUser} xe.");

        vehicle.Id = 0;
        vehicle.UserId = userId;
        vehicle.Make = vehicle.Make.Trim();
        vehicle.CarModel = vehicle.CarModel.Trim();
        vehicle.CreatedAt = DateTime.Now;
        vehicle.KmUpdatedAt = DateTime.Now;

        _db.UserVehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã thêm xe {vehicle.DisplayName}.");
    }

    public async Task<ServiceResult> UpdateKmAsync(int id, string userId, int km)
    {
        var v = await _db.UserVehicles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (v == null) return ServiceResult.Failure("Không tìm thấy xe.");
        if (km < 0 || km > 2_000_000) return ServiceResult.Failure("Số km không hợp lệ.");

        v.CurrentKm = km;
        v.KmUpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return ServiceResult.Success(string.Format(Vi, "Đã cập nhật số km: {0:N0} km.", km));
    }

    public async Task<ServiceResult> DeleteVehicleAsync(int id, string userId)
    {
        var v = await _db.UserVehicles.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (v == null) return ServiceResult.Failure("Không tìm thấy xe.");

        _db.UserVehicles.Remove(v);   // lịch sử bảo dưỡng bị xóa theo (cascade)
        await _db.SaveChangesAsync();
        return ServiceResult.Success($"Đã xóa xe {v.DisplayName}.");
    }

    public async Task<ServiceResult> MarkDoneAsync(int vehicleId, string userId, string itemKey, int? km, DateTime? date)
    {
        var item = MaintenanceSchedule.Find(itemKey);
        if (item == null) return ServiceResult.Failure("Hạng mục không hợp lệ.");

        var v = await _db.UserVehicles.Include(x => x.Records)
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.UserId == userId);
        if (v == null) return ServiceResult.Failure("Không tìm thấy xe.");

        var doneKm = km ?? v.CurrentKm;
        if (doneKm < 0 || doneKm > v.CurrentKm)
            return ServiceResult.Failure("Số km lúc thay phải từ 0 đến số km hiện tại của xe.");

        var doneDate = (date ?? DateTime.Today).Date;
        if (doneDate > DateTime.Today)
            return ServiceResult.Failure("Ngày thực hiện không được ở tương lai.");

        var record = v.Records.FirstOrDefault(r => r.ItemKey == itemKey);
        if (record == null)
        {
            record = new MaintenanceRecord { ItemKey = itemKey };
            v.Records.Add(record);
        }
        record.LastKm = doneKm;
        record.LastDate = doneDate;

        await _db.SaveChangesAsync();
        return ServiceResult.Success(string.Format(Vi, "Đã ghi nhận \"{0}\" ở {1:N0} km.", item.Name, doneKm));
    }

    // ===== Tính lịch bảo dưỡng =====

    public async Task<VehicleMaintenanceSummary> GetSummaryAsync(UserVehicle vehicle)
    {
        var products = await LoadProductsAsync();
        var summary = new VehicleMaintenanceSummary { Vehicle = vehicle };

        foreach (var item in MaintenanceSchedule.Items)
        {
            var status = Evaluate(vehicle, item);
            status.Suggestions = Suggest(products, item, vehicle);
            summary.Items.Add(status);
        }

        // Ưu tiên: quá hạn -> sắp đến hạn -> chưa ghi nhận -> còn xa
        summary.Items = summary.Items
            .OrderByDescending(s => s.State)
            .ThenBy(s => s.RemainingKm ?? int.MaxValue)
            .ToList();
        return summary;
    }

    public async Task<int> CountDueAsync(string userId)
    {
        var vehicles = await GetVehiclesAsync(userId);
        return vehicles.Sum(v => MaintenanceSchedule.Items.Count(i => Evaluate(v, i).NeedsAttention));
    }

    /// <summary>Tính mốc tiếp theo và trạng thái của 1 hạng mục.</summary>
    private static MaintenanceStatus Evaluate(UserVehicle v, MaintenanceItem item)
    {
        var record = v.Records.FirstOrDefault(r => r.ItemKey == item.Key);
        var status = new MaintenanceStatus
        {
            Item = item,
            HasRecord = record != null,
            LastKm = record?.LastKm,
            LastDate = record?.LastDate
        };

        var state = MaintenanceState.Ok;

        // --- Theo số km ---
        if (item.IntervalKm is int interval)
        {
            int nextKm;
            if (record != null)
            {
                nextKm = record.LastKm + interval;
            }
            else
            {
                // Chưa có lịch sử: lấy mốc bảo dưỡng tròn tiếp theo (vd 5.000, 10.000, 15.000...)
                nextKm = (v.CurrentKm / interval + 1) * interval;
            }

            status.NextKm = nextKm;
            status.RemainingKm = nextKm - v.CurrentKm;
            var warnZone = Math.Max(500, interval / 10);

            if (status.RemainingKm < 0) state = MaintenanceState.Overdue;
            else if (status.RemainingKm <= warnZone) state = MaintenanceState.DueSoon;
        }

        // --- Theo thời gian ---
        if (item.IntervalMonths is int months && record?.LastDate is DateTime last)
        {
            status.NextDate = last.AddMonths(months);
            if (DateTime.Today > status.NextDate) state = Max(state, MaintenanceState.Overdue);
            else if (DateTime.Today.AddDays(30) >= status.NextDate) state = Max(state, MaintenanceState.DueSoon);
        }

        // Chưa ghi nhận lần nào và chưa tới mức cảnh báo -> "chưa rõ"
        if (record == null && state == MaintenanceState.Ok)
            state = MaintenanceState.Unknown;

        status.State = state;
        return status;
    }

    private static MaintenanceState Max(MaintenanceState a, MaintenanceState b) => a > b ? a : b;

    // ===== Gợi ý phụ tùng =====

    private record ProductInfo(Product Product, string Text);

    private async Task<List<ProductInfo>> LoadProductsAsync()
    {
        var list = await _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync();
        return list.Select(p => new ProductInfo(p, Normalize(p.Name))).ToList();
    }

    /// <summary>
    /// Chọn phụ tùng khớp hạng mục. Ưu tiên sản phẩm ghi đúng dòng xe / hãng xe của khách, còn hàng.
    /// </summary>
    private static List<Product> Suggest(List<ProductInfo> products, MaintenanceItem item, UserVehicle v)
    {
        var keywords = item.Keywords.Select(Normalize).ToList();
        var model = Normalize(v.CarModel);
        var make = Normalize(v.Make);

        return products
            .Where(p => keywords.Any(k => p.Text.Contains(k)))
            .Select(p => new
            {
                p.Product,
                Score = (model.Length > 0 && p.Text.Contains(model) ? 3 : 0)
                      + (make.Length > 0 && p.Text.Contains(make) ? 1 : 0)
                      + (p.Product.Stock > 0 ? 2 : 0)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Price)
            .Take(SuggestionsPerItem)
            .Select(x => x.Product)
            .ToList();
    }

    /// <summary>Chữ thường, bỏ dấu tiếng Việt.</summary>
    private static string Normalize(string s)
    {
        var d = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Trim();
    }
}
