using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class BillPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public Guid ProviderId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public BillPaymentStatus Status { get; set; } = BillPaymentStatus.Pending;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public BillProvider Provider { get; set; } = null!;
}
