using System.ComponentModel.DataAnnotations;

namespace TeamManager.API.Models.DTOs;

public class ProjectCreateDto
{
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
