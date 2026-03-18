namespace backend.Domain.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Subject { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
