namespace SomoniBank.Domain.DTOs;

public class RecurringPaymentInsertDto
{
    public Guid AccountId { get; set; }
    public string ProviderName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public DateTime NextExecutionDate { get; set; }
    public string? Notes { get; set; }
}

public class RecurringPaymentGetDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string ProviderName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public DateTime NextExecutionDate { get; set; }
    public DateTime? LastExecutionDate { get; set; }
    public string Status { get; set; } = null!;
    public int AutoRetryCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecurringPaymentHistoryGetDto
{
    public Guid Id { get; set; }
    public Guid? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;
    public int RetryAttempt { get; set; }
    public DateTime ExecutedAt { get; set; }
}
