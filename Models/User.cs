namespace TaskFlowAPI.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public enum Role
{
    User,
    Admin
}
