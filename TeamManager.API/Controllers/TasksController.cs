using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamManager.API.Data;
using TeamManager.API.Models;
using TeamManager.API.Models.DTOs;
using TeamManager.API.Services;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _log;
    private readonly NotificationService _noti;

    public TasksController(AppDbContext context, ActivityLogService log, NotificationService noti)
    {
        _context = context;
        _log = log;
        _noti = noti;
    }

    private static object ToUi(TaskItem t) => new
    {
        id = t.Id,
        title = t.Title,
        description = t.Description,
        status = t.Status,
        projectId = t.ProjectId,
        assignedUserId = t.AssignedUserId,
        assignedUserName = t.AssignedUser == null ? null : t.AssignedUser.Name,
        createdAt = t.CreatedAt,
        dueDate = t.DueDate,
        completedAt = t.CompletedAt,
    };

    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst("UserId")?.Value;

        int userId = 0;
        if (!string.IsNullOrWhiteSpace(userIdClaim))
            int.TryParse(userIdClaim, out userId);

        var query = _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.AssignedUser)
            .AsNoTracking();

        if (role == "Junior")
            query = query.Where(t => t.AssignedUserId == userId);

        var tasks = await query
            .OrderByDescending(t => t.Id)
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                description = t.Description,
                status = t.Status,
                projectId = t.ProjectId,
                assignedUserId = t.AssignedUserId,
                assignedUserName = t.AssignedUser == null ? null : t.AssignedUser.Name,
                createdAt = t.CreatedAt,
                dueDate = t.DueDate
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    [Authorize(Roles = "Lead,Admin")]
    public async Task<IActionResult> Create([FromBody] TaskCreateDto dto)
    {
        if (dto == null) return BadRequest("Body is required.");
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
        if (!projectExists)
            return BadRequest("Invalid ProjectId.");

        DateTime? dueUtc = null;
        if (dto.DueDate.HasValue)
        {
            dueUtc = dto.DueDate.Value.Kind == DateTimeKind.Utc
                ? dto.DueDate.Value
                : DateTime.SpecifyKind(dto.DueDate.Value, DateTimeKind.Utc);
        }

        var task = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ProjectId = dto.ProjectId,
            Status = "Todo",
            AssignedUserId = null,
            CreatedAt = DateTime.UtcNow,
            DueDate = dueUtc
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var created = await _context.Tasks
            .Include(t => t.AssignedUser)
            .AsNoTracking()
            .FirstAsync(t => t.Id == task.Id);

        return Ok(ToUi(created));
    }

    [HttpPut("{taskId:int}/assign/{userId:int}")]
    [Authorize(Roles = "Lead,Admin")]
    public async Task<IActionResult> Assign(int taskId, int userId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return NotFound();

        if (userId == 0)
        {
            task.AssignedUserId = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return BadRequest("Invalid userId.");

        task.AssignedUserId = userId;
        await _context.SaveChangesAsync();

        await _noti.CreateAsync(
            userId,
            "Yeni görev atandı",
            $"\"{task.Title}\" görevi sana atandı.",
            $"/projects/{task.ProjectId}"
        );

        return NoContent();
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = "Todo";
    }

    [HttpPut("{taskId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int taskId, [FromBody] UpdateStatusDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
            return BadRequest("status is required.");

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return NotFound();

        var allowed = new[] { "Todo", "Doing", "Done" };
        if (!allowed.Contains(dto.Status)) return BadRequest("Invalid status.");

        var role = User.FindFirst(ClaimTypes.Role)?.Value
                   ?? User.FindFirst("role")?.Value
                   ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

        var userIdClaim = User.FindFirst("UserId")?.Value;
        int.TryParse(userIdClaim, out var meId);

        if (role == "Junior" && task.AssignedUserId != meId)
            return Forbid();

        var oldStatus = task.Status;
        task.Status = dto.Status;

        if (dto.Status == "Done" && oldStatus != "Done")
        {
            task.CompletedAt = DateTime.UtcNow;
        }

        if (oldStatus == "Done" && dto.Status != "Done")
        {
            task.CompletedAt = null;
        }

        await _context.SaveChangesAsync();

        if (task.AssignedUserId.HasValue && oldStatus != dto.Status)
        {
            await _noti.CreateAsync(
                task.AssignedUserId.Value,
                "Görev durumu güncellendi",
                $"\"{task.Title}\" durumu: {oldStatus} → {dto.Status}",
                $"/projects/{task.ProjectId}"
            );
        }

        return NoContent();
    }

    [HttpDelete("{taskId:int}")]
    [Authorize(Roles = "Lead,Admin")]
    public async Task<IActionResult> Delete(int taskId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

