namespace SomoniBank.Domain.DTOs;

public class SpendingByCategoryDto
{
    public string Category { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class MonthlyStatsDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = null!;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance { get; set; }
}

public class IncomeVsExpenseDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance { get; set; }
    public decimal SavingsRate { get; set; }
}

public class TopRecipientDto
{
    public string AccountNumber { get; set; } = null!;
    public int TransferCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class AnalyticsPeriodDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? AccountId { get; set; }
}
