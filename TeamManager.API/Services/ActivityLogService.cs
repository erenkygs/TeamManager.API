using TeamManager.API.Data;
using TeamManager.API.Models;

namespace TeamManager.API.Services;

public class ActivityLogService
{
    private readonly AppDbContext _context;

    public ActivityLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string action, string entityType, int entityId)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
