namespace TeamManager.API.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public string Status { get; set; } = "Todo"; // Todo / Doing / Done

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }
}
