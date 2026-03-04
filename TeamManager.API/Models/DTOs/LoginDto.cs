using System.ComponentModel.DataAnnotations;

namespace TeamManager.API.Models.DTOs;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(6)]
    public string Password { get; set; } = null!;
}
