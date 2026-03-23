namespace SomoniBank.Domain.DTOs;

public class NotificationGetDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public string Type { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}