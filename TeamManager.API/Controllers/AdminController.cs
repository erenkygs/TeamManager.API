using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamManager.API.Data;
using TeamManager.API.Models.DTOs;
using TeamManager.API.Services;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ActivityLogService _log;

    public AdminController(AppDbContext context, ActivityLogService log)
    {
        _context = context;
        _log = log;
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found");

        dto.Role = dto.Role.Trim();

        var allowedRoles = new[] { "Junior", "Lead", "Admin" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest("Invalid role. Allowed: Junior, Lead, Admin");

        user.Role = dto.Role;
        await _context.SaveChangesAsync();

        var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
        await _log.LogAsync(currentUserId, $"Updated User Role -> {dto.Role}", "User", id);

        return Ok($"User role updated to {dto.Role}");
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.Title, u.CreatedAt })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _context.ActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .ToListAsync();

        return Ok(logs);
    }
}
