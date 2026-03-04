using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamManager.API.Data;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return Unauthorized("UserId claim missing. Please login again.");

        var userId = int.Parse(userIdClaim);
        var now = DateTime.UtcNow;

        var taskQuery = _context.Tasks.AsQueryable();
        if (role == "Junior")
            taskQuery = taskQuery.Where(t => t.AssignedUserId == userId);

        var totalTasks = await taskQuery.CountAsync();
        var todoTasks = await taskQuery.CountAsync(t => t.Status == "Todo");
        var doingTasks = await taskQuery.CountAsync(t => t.Status == "Doing");
        var doneTasks = await taskQuery.CountAsync(t => t.Status == "Done");

        var overdueTasks = await taskQuery.CountAsync(t =>
            t.DueDate != null && t.DueDate < now && t.Status != "Done");
        var totalProjects = await _context.Projects.CountAsync();

        var activeProjectsQuery = _context.Tasks.Where(t => t.Status != "Done");
        if (role == "Junior")
            activeProjectsQuery = activeProjectsQuery.Where(t => t.AssignedUserId == userId);

        var activeProjects = await activeProjectsQuery
            .Select(t => t.ProjectId)
            .Distinct()
            .CountAsync();
        var leaderboardRaw = await taskQuery
            .Where(t => t.AssignedUserId != null && t.Status == "Done")
            .GroupBy(t => t.AssignedUserId)
            .Select(g => new { userId = g.Key!.Value, completed = g.Count() })
            .OrderByDescending(x => x.completed)
            .Take(10)
            .ToListAsync();

        var lbUserIds = leaderboardRaw.Select(x => x.userId).ToList();

        var lbUsers = await _context.Users
            .Where(u => lbUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name, u.Email })
            .ToDictionaryAsync(x => x.Id, x => new { x.Name, x.Email });

        var leaderboard = leaderboardRaw.Select(x => new
        {
            userId = x.userId,
            name = lbUsers.ContainsKey(x.userId) ? lbUsers[x.userId].Name : null,
            email = lbUsers.ContainsKey(x.userId) ? lbUsers[x.userId].Email : null,
            completed = x.completed
        });
        var start = DateTime.UtcNow.Date.AddDays(-6); 
        var end = DateTime.UtcNow.Date.AddDays(1);

        var doneLogsQuery =
            from log in _context.ActivityLogs
            join t in _context.Tasks on log.EntityId equals t.Id
            where log.EntityType == "Task"
                  && log.Action.Contains("Updated Task Status -> Done")
                  && log.Timestamp >= start && log.Timestamp < end
            select new { log.Timestamp, t.AssignedUserId };

        if (role == "Junior")
            doneLogsQuery = doneLogsQuery.Where(x => x.AssignedUserId == userId);

        var doneLogs = await doneLogsQuery.ToListAsync();

        var completedLast7Days = Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .Select(day => new
            {
                date = day.ToString("yyyy-MM-dd"),
                completed = doneLogs.Count(x => x.Timestamp.Date == day.Date)
            })
            .ToList();

        return Ok(new
        {
            totalTasks,
            todoTasks,
            doingTasks,
            doneTasks,
            overdueTasks,

            totalProjects,
            activeProjects,

            leaderboard,
            completedLast7Days
        });
    }

    [HttpGet("projects/{projectId}/summary")]
    public async Task<IActionResult> ProjectSummary(int projectId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst("UserId")?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return Unauthorized("UserId claim missing. Please login again.");

        var userId = int.Parse(userIdClaim);
        var now = DateTime.UtcNow;

        var taskQuery = _context.Tasks.Where(t => t.ProjectId == projectId);
        if (role == "Junior")
            taskQuery = taskQuery.Where(t => t.AssignedUserId == userId);

        var totalTasks = await taskQuery.CountAsync();
        var todoTasks = await taskQuery.CountAsync(t => t.Status == "Todo");
        var doingTasks = await taskQuery.CountAsync(t => t.Status == "Doing");
        var doneTasks = await taskQuery.CountAsync(t => t.Status == "Done");

        var overdueTasks = await taskQuery.CountAsync(t =>
            t.DueDate != null && t.DueDate < now && t.Status != "Done");

        var start = DateTime.UtcNow.Date.AddDays(-6);
        var end = DateTime.UtcNow.Date.AddDays(1);

        var doneLogsQuery =
            from log in _context.ActivityLogs
            join t in _context.Tasks on log.EntityId equals t.Id
            where log.EntityType == "Task"
                  && log.Action.Contains("Updated Task Status -> Done")
                  && log.Timestamp >= start && log.Timestamp < end
                  && t.ProjectId == projectId
            select new { log.Timestamp, t.AssignedUserId };

        if (role == "Junior")
            doneLogsQuery = doneLogsQuery.Where(x => x.AssignedUserId == userId);

        var doneLogs = await doneLogsQuery.ToListAsync();

        var completedLast7Days = Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .Select(day => new
            {
                date = day.ToString("yyyy-MM-dd"),
                completed = doneLogs.Count(x => x.Timestamp.Date == day.Date)
            })
            .ToList();

        return Ok(new
        {
            projectId,
            totalTasks,
            todoTasks,
            doingTasks,
            doneTasks,
            overdueTasks,
            completedLast7Days
        });
    }
}
