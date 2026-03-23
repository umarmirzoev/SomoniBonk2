using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class FraudAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? TransactionId { get; set; }
    public string Reason { get; set; } = null!;
    public int RiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public FraudStatus Status { get; set; } = FraudStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
