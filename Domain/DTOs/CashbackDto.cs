namespace SomoniBank.Domain.DTOs;

public class CashbackGetDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class CashbackSummaryDto
{
    public decimal TotalCashback { get; set; }
    public decimal AvailableBalance { get; set; }
    public int TotalTransactions { get; set; }
}