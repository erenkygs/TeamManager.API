using System.ComponentModel.DataAnnotations;

namespace TeamManager.API.Models.DTOs;

public class RegisterDto
{
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(6)]
    public string Password { get; set; } = null!;

    public string? Role { get; set; }

    public string? Title { get; set; }
}