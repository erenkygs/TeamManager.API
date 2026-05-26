using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamManager.API.Data;
using TeamManager.API.Models;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/status-posts")]
[Authorize]
public class StatusPostsController : ControllerBase
{
    private readonly AppDbContext _context;
    public StatusPostsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var posts = await _context.StatusPosts
            .Include(p => p.User)
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new
            {
                p.Id,
                p.UserId,
                userName = p.User.Name ?? p.User.Email,
                p.Message,
                createdAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStatusPostDto dto)
    {
        var msg = (dto.Message ?? "").Trim();
        if (string.IsNullOrEmpty(msg) || msg.Length > 120)
            return BadRequest("Mesaj 1-120 karakter arasında olmalı.");

        var userId = int.Parse(User.FindFirstValue("UserId")!);

        var post = new StatusPost { UserId = userId, Message = msg, CreatedAt = DateTime.UtcNow };
        _context.StatusPosts.Add(post);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return Ok(new
        {
            post.Id,
            post.UserId,
            userName = user?.Name ?? user?.Email,
            post.Message,
            createdAt = post.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue("UserId")!);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";

        var post = await _context.StatusPosts.FindAsync(id);
        if (post == null) return NotFound();
        if (post.UserId != userId && role != "Admin")
            return Forbid();

        _context.StatusPosts.Remove(post);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateStatusPostDto
{
    public string? Message { get; set; }
}
