namespace SomoniBank.Domain.DTOs;

public class TransactionLimitInsertDto
{
    public Guid AccountId { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal SingleTransactionLimit { get; set; }
}

public class TransactionLimitGetDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal SingleTransactionLimit { get; set; }
    public decimal UsedTodayAmount { get; set; }
    public DateTime UpdatedAt { get; set; }
}