using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TeamManager.API.Data;
using TeamManager.API.Models;
using TeamManager.API.Models.DTOs;
using TeamManager.API.Services;

namespace TeamManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;

    public AuthController(AppDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin,Lead")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        dto.Email = dto.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Email already exists");

        var role = (dto.Role ?? "Junior").Trim();

        if (role.Equals("Member", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Üye", StringComparison.OrdinalIgnoreCase))
        {
            role = "Junior";
        }

        var allowedRoles = new[] { "Junior", "Lead", "Admin" };
        if (!allowedRoles.Contains(role))
            return BadRequest("Invalid role. Allowed: Junior, Lead, Admin");

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok("User created");
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        dto.Email = dto.Email.Trim().ToLower();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = _authService.CreateToken(user);

        return Ok(new { token });
    }

    public class ChangePasswordPublicDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
        [Required]
        public string CurrentPassword { get; set; } = null!;
        [Required, MinLength(6)]
        public string NewPassword { get; set; } = null!;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordPublicDto dto)
    {
        dto.Email = dto.Email.Trim().ToLower();

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest("Email veya mevcut şifre hatalı.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return Ok("Şifre başarıyla güncellendi.");
    }

}
