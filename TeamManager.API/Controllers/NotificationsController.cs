using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamManager.API.Data;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsController(AppDbContext db) => _db = db;

    int GetMeId()
    {
        var idStr = User.FindFirst("UserId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(idStr, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetMy([FromQuery] int take = 30)
    {
        var meId = GetMeId();
        if (meId == 0) return Unauthorized();

        take = Math.Clamp(take, 1, 100);

        var items = await _db.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == meId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new {
                id = x.Id,
                title = x.Title,
                message = x.Message,
                link = x.Link,
                isRead = x.IsRead,
                createdAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var meId = GetMeId();
        if (meId == 0) return Unauthorized();

        var count = await _db.Notifications.CountAsync(x => x.UserId == meId && !x.IsRead);
        return Ok(new { count });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var meId = GetMeId();
        if (meId == 0) return Unauthorized();

        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == meId);
        if (n == null) return NotFound();

        n.IsRead = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> ReadAll()
    {
        var meId = GetMeId();
        if (meId == 0) return Unauthorized();

        await _db.Notifications
            .Where(x => x.UserId == meId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));

        return NoContent();
    }
}