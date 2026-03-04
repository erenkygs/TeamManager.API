using System.ComponentModel.DataAnnotations;

namespace TeamManager.API.Models.DTOs;

public class TaskCreateDto
{
    [Required, MinLength(2)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public int ProjectId { get; set; }

    public DateTime? DueDate { get; set; }
}
