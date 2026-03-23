namespace SomoniBank.Domain.Models;

public class SupportMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid? SenderUserId { get; set; }
    public Guid? SenderAdminId { get; set; }
    public string MessageText { get; set; } = null!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    public SupportTicket Ticket { get; set; } = null!;
    public User? SenderUser { get; set; }
    public User? SenderAdmin { get; set; }
}
