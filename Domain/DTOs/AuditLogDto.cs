namespace SomoniBank.Domain.DTOs;

public class AuditLogGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; }
}