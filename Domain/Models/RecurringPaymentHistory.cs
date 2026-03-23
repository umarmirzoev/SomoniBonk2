namespace SomoniBank.Domain.Models;

public class RecurringPaymentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecurringPaymentId { get; set; }
    public Guid? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;
    public int RetryAttempt { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public RecurringPayment RecurringPayment { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
