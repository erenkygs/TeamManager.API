using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamManager.API.Data;
using TeamManager.API.Models;
using TeamManager.API.Models.DTOs;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/tasks/{taskId:int}/comments")]
[Authorize]
public class TaskCommentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TaskCommentsController(AppDbContext db) => _db = db;

    private int GetMeId()
    {
        var idStr = User.FindFirst("UserId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out var id) ? id : 0;
    }

    private string GetRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value
            ?? User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value
            ?? "";
    }

    [HttpGet]
    public async Task<IActionResult> List(int taskId)
    {
        var exists = await _db.Tasks.AnyAsync(t => t.Id == taskId);
        if (!exists) return NotFound("Task not found");

        var items = await _db.TaskComments
            .AsNoTracking()
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                id = c.Id,
                taskId = c.TaskId,
                userId = c.UserId,
                authorName = c.User.Name,
                authorAvatarUrl = c.User.AvatarUrl,
                text = c.Text,
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int taskId, [FromBody] CreateTaskCommentDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Text))
            return BadRequest("Text is required.");

        var meId = GetMeId();
        if (meId == 0) return Unauthorized();

        var task = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return NotFound("Task not found");

        var comment = new TaskComment
        {
            TaskId = taskId,
            UserId = meId,
            Text = dto.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.TaskComments.Add(comment);
        await _db.SaveChangesAsync();

        var created = await _db.TaskComments
            .AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(c => new
            {
                id = c.Id,
                taskId = c.TaskId,
                userId = c.UserId,
                authorName = c.User.Name,
                authorAvatarUrl = c.User.AvatarUrl,
                text = c.Text,
                createdAt = c.CreatedAt
            })
            .FirstAsync();

        return Ok(created);
    }

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int taskId, int commentId)
    {
        var meId = GetMeId();
        if (meId == 0) return Unauthorized();

        var role = GetRole();

        var c = await _db.TaskComments.FirstOrDefaultAsync(x => x.Id == commentId && x.TaskId == taskId);
        if (c == null) return NotFound();

        var canDelete = c.UserId == meId || role == "Admin" || role == "Lead";
        if (!canDelete) return Forbid();

        _db.TaskComments.Remove(c);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}