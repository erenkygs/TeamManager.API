using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamManager.API.Data;
using TeamManager.API.Models;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeController : ControllerBase
{
    private readonly AppDbContext _context;

    public KnowledgeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _context.KnowledgeItems
            .Include(k => k.Author)
            .OrderByDescending(k => k.UpdatedAt)
            .Select(k => new
            {
                k.Id,
                k.Question,
                k.Answer,
                k.AuthorId,
                authorName = k.Author.Name,
                k.CreatedAt,
                k.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KnowledgeDto dto)
    {
        var userId = int.Parse(User.FindFirstValue("UserId")!);

        var item = new KnowledgeItem
        {
            Question = dto.Question.Trim(),
            Answer = dto.Answer.Trim(),
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.KnowledgeItems.Add(item);
        await _context.SaveChangesAsync();

        var author = await _context.Users.FindAsync(userId);

        return Ok(new
        {
            item.Id,
            item.Question,
            item.Answer,
            item.AuthorId,
            authorName = author?.Name,
            item.CreatedAt,
            item.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] KnowledgeDto dto)
    {
        var userId = int.Parse(User.FindFirstValue("UserId")!);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";

        var item = await _context.KnowledgeItems.FindAsync(id);
        if (item == null) return NotFound();

        if (item.AuthorId != userId && role != "Admin" && role != "Lead")
            return Forbid();

        item.Question = dto.Question.Trim();
        item.Answer = dto.Answer.Trim();
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue("UserId")!);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";

        var item = await _context.KnowledgeItems.FindAsync(id);
        if (item == null) return NotFound();

        if (item.AuthorId != userId && role != "Admin" && role != "Lead")
            return Forbid();

        _context.KnowledgeItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class KnowledgeDto
{
    public string Question { get; set; } = null!;
    public string Answer { get; set; } = null!;
}
