namespace SomoniBank.Domain.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Action { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}