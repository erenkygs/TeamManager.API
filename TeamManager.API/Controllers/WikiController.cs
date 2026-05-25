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
public class WikiController : ControllerBase
{
    private readonly AppDbContext _context;
    public WikiController(AppDbContext context) => _context = context;

    private int CurrentUserId => int.Parse(User.FindFirstValue("UserId") ?? "0");
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var articles = await _context.WikiArticles
            .Include(a => a.Author)
            .OrderByDescending(a => a.UpdatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                Preview = a.Content.Length > 180 ? a.Content.Substring(0, 180) + "…" : a.Content,
                AuthorName = a.Author.Name ?? a.Author.Email,
                a.AuthorId,
                a.CreatedAt,
                a.UpdatedAt
            })
            .AsNoTracking()
            .ToListAsync();

        return Ok(articles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var article = await _context.WikiArticles
            .Include(a => a.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null) return NotFound();

        return Ok(new
        {
            article.Id,
            article.Title,
            article.Content,
            AuthorName = article.Author.Name ?? article.Author.Email,
            article.AuthorId,
            article.CreatedAt,
            article.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WikiArticleDto dto)
    {
        var article = new WikiArticle
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            AuthorId = CurrentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WikiArticles.Add(article);
        await _context.SaveChangesAsync();
        return Ok(new { article.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] WikiArticleDto dto)
    {
        var article = await _context.WikiArticles.FindAsync(id);
        if (article == null) return NotFound();
        if (article.AuthorId != CurrentUserId && CurrentRole != "Admin") return Forbid();

        article.Title = dto.Title.Trim();
        article.Content = dto.Content.Trim();
        article.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _context.WikiArticles.FindAsync(id);
        if (article == null) return NotFound();
        if (article.AuthorId != CurrentUserId && CurrentRole != "Admin") return Forbid();

        _context.WikiArticles.Remove(article);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class WikiArticleDto
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}
