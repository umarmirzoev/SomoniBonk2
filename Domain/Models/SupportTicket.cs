using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Subject { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Description { get; set; } = null!;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public SupportPriority Priority { get; set; } = SupportPriority.Medium;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public Guid? AssignedAdminId { get; set; }

    public User User { get; set; } = null!;
    public User? AssignedAdmin { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}
