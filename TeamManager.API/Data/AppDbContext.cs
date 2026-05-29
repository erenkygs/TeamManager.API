using Microsoft.EntityFrameworkCore;
using TeamManager.API.Models;


namespace TeamManager.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<StatusPost> StatusPosts => Set<StatusPost>();


}
