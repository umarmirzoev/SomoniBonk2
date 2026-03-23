using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class CreditScoreHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal MonthlyIncome { get; set; }
    public string EmploymentStatus { get; set; } = null!;
    public decimal ExistingDebt { get; set; }
    public int CreditHistoryLengthMonths { get; set; }
    public int MissedPaymentsCount { get; set; }
    public int CurrentLoanCount { get; set; }
    public decimal AccountTurnover { get; set; }
    public int ScoreValue { get; set; }
    public CreditDecision Decision { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
}
