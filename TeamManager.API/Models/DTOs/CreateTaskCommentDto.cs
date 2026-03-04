using System.ComponentModel.DataAnnotations;

namespace TeamManager.API.Models.DTOs;

public class CreateTaskCommentDto
{
    [Required, MinLength(1), MaxLength(2000)]
    public string Text { get; set; } = null!;
}