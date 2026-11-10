using AutoPartsManagement.Data.Context;
using AutoPartsManagement.Data.Models;
using AutoPartsManagement.Services.Common;
using AutoPartsManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<Notification>> GetLatestAsync(int take = 8) =>
        _db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.Id)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync();

    public Task<int> CountUnreadAsync() => _db.Notifications.CountAsync(n => !n.IsRead);

    public Task<PagedResult<Notification>> GetPagedAsync(bool unreadOnly, int page, int pageSize)
    {
        var query = _db.Notifications.AsNoTracking();
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        return query.OrderByDescending(n => n.Id).ToPagedResultAsync(page, pageSize);
    }

    public async Task<Notification?> MarkReadAsync(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null) return null;
        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return n;
    }

    public async Task<int> MarkAllReadAsync()
    {
        var unread = await _db.Notifications.Where(n => !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        return unread.Count;
    }
}
