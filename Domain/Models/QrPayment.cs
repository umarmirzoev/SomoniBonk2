using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class QrPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FromUserId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public string QrCode { get; set; } = null!;
    public QrPaymentStatus Status { get; set; } = QrPaymentStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? FromUser { get; set; }
    public Account ToAccount { get; set; } = null!;
}
