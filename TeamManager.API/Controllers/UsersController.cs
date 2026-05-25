using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamManager.API.Data;
using System.Security.Claims;
using TeamManager.API.Models.DTOs;


namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) { _db = db; }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .AsNoTracking()
            .Select(u => new {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                role = u.Role,
                title = u.Title,
                avatarUrl = u.AvatarUrl
            })
            .OrderBy(u => u.name)
            .ToListAsync();

        return Ok(users);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirst("UserId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var u = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new {
                id = x.Id,
                name = x.Name,
                email = x.Email,
                role = x.Role,
                title = x.Title,
                avatarUrl = x.AvatarUrl,
                createdAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (u == null) return NotFound();
        return Ok(u);
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest("Şifre alanları zorunlu.");

        if (dto.NewPassword.Length < 6)
            return BadRequest("Yeni şifre en az 6 karakter olmalı.");

        var userIdStr = User.FindFirst("UserId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();
        var hash = user.PasswordHash; 

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, hash))
            return BadRequest("Mevcut şifre yanlış.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var meIdClaim = User.FindFirstValue("UserId");

        if (int.TryParse(meIdClaim, out var meId) && meId == id)
            return BadRequest("Kendi hesabını silemezsin.");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
