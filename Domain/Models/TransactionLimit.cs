namespace SomoniBank.Domain.Models;

public class TransactionLimit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public decimal DailyLimit { get; set; } = 10000;
    public decimal SingleTransactionLimit { get; set; } = 5000;
    public decimal UsedTodayAmount { get; set; } = 0;
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}