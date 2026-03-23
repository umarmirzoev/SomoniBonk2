using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class RecurringPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string ProviderName { get; set; } = null!;
    public RecurringPaymentCategory Category { get; set; }
    public decimal Amount { get; set; }
    public Currency CurrencyCode { get; set; }
    public RecurringPaymentFrequency Frequency { get; set; }
    public DateTime NextExecutionDate { get; set; }
    public DateTime? LastExecutionDate { get; set; }
    public RecurringPaymentStatus Status { get; set; } = RecurringPaymentStatus.Active;
    public int AutoRetryCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public ICollection<RecurringPaymentHistory> History { get; set; } = new List<RecurringPaymentHistory>();
}
