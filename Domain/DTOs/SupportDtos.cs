namespace SomoniBank.Domain.DTOs;

public class SupportTicketInsertDto
{
    public string Subject { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Priority { get; set; } = null!;
}

public class SupportTicketGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? AssignedAdminId { get; set; }
}

public class SupportMessageInsertDto
{
    public string MessageText { get; set; } = null!;
}

public class SupportMessageGetDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid? SenderUserId { get; set; }
    public Guid? SenderAdminId { get; set; }
    public string MessageText { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class SupportTicketStatusUpdateDto
{
    public string Status { get; set; } = null!;
}
