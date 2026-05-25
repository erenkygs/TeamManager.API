using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamManager.API.Data;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Lead")]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _context;
    public SessionsController(AppDbContext context) => _context = context;

    [HttpGet("report")]
    public async Task<IActionResult> Report([FromQuery] int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var sessions = await _context.UserSessions
            .Where(s => s.LoginAt >= since)
            .Include(s => s.User)
            .AsNoTracking()
            .ToListAsync();

        var now = DateTime.UtcNow;
        var rows = sessions
            .Select(s => new
            {
                s.UserId,
                UserName = s.User.Name ?? s.User.Email,
                Date = s.LoginAt.Date,
                Minutes = (int)((s.LogoutAt ?? (s.LoginAt.Date < now.Date ? s.LoginAt.Date.AddDays(1) : now)) - s.LoginAt).TotalMinutes
            })
            .GroupBy(x => new { x.UserId, x.UserName, x.Date })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.UserName,
                date = g.Key.Date.ToString("yyyy-MM-dd"),
                totalMinutes = g.Sum(x => x.Minutes)
            })
            .OrderByDescending(x => x.date)
            .ThenBy(x => x.UserName)
            .ToList();

        return Ok(rows);
    }
}
