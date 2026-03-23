namespace SomoniBank.Domain.DTOs;

public class FraudAlertGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TransactionId { get; set; }
    public string Reason { get; set; } = null!;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? Notes { get; set; }
}

public class FraudAlertReviewDto
{
    public string? Notes { get; set; }
}
