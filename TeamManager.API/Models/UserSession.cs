namespace TeamManager.API.Models;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
}
