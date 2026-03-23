namespace SomoniBank.Domain.DTOs;

public class CreditScoreCalculateDto
{
    public decimal MonthlyIncome { get; set; }
    public string EmploymentStatus { get; set; } = null!;
    public decimal ExistingDebt { get; set; }
    public int CreditHistoryLengthMonths { get; set; }
    public int MissedPaymentsCount { get; set; }
    public decimal? RequestedAmount { get; set; }
    public string? Notes { get; set; }
}

public class CreditScoreResultDto
{
    public Guid UserId { get; set; }
    public decimal MonthlyIncome { get; set; }
    public string EmploymentStatus { get; set; } = null!;
    public decimal ExistingDebt { get; set; }
    public int CreditHistoryLengthMonths { get; set; }
    public int MissedPaymentsCount { get; set; }
    public int CurrentLoanCount { get; set; }
    public decimal AccountTurnover { get; set; }
    public int ScoreValue { get; set; }
    public string Decision { get; set; } = null!;
    public DateTime CalculatedAt { get; set; }
    public string? Notes { get; set; }
    public string Explanation { get; set; } = null!;
}
