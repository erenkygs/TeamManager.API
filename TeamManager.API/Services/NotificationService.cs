using Microsoft.EntityFrameworkCore;
using TeamManager.API.Data;
using TeamManager.API.Models;

namespace TeamManager.API.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public async Task CreateAsync(int userId, string title, string message, string? link = null)
    {
        var n = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Link = link
        };

        _db.Notifications.Add(n);

        
        await _db.SaveChangesAsync();

        var idsToDelete = await _db.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(200)
            .Select(x => x.Id)
            .ToListAsync();

        if (idsToDelete.Count > 0)
        {
            _db.Notifications.RemoveRange(_db.Notifications.Where(x => idsToDelete.Contains(x.Id)));
            await _db.SaveChangesAsync();
        }
    }
}