namespace backend.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Keycloak "sub"
    public string Subject { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
