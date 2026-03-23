namespace SomoniBank.Domain.DTOs;

public class VirtualCardInsertDto
{
    public Guid LinkedAccountId { get; set; }
    public string CardHolderName { get; set; } = null!;
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public bool IsSingleUse { get; set; }
}

public class VirtualCardGetDto
{
    public Guid Id { get; set; }
    public Guid LinkedAccountId { get; set; }
    public string CardHolderName { get; set; } = null!;
    public string MaskedCardNumber { get; set; } = null!;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public bool IsSingleUse { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class VirtualCardCreateResultDto
{
    public VirtualCardGetDto Card { get; set; } = null!;
    public string GeneratedCvv { get; set; } = null!;
}

public class VirtualCardLimitUpdateDto
{
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
}

public class VirtualCardPaymentDto
{
    public Guid VirtualCardId { get; set; }
    public decimal Amount { get; set; }
    public string MerchantName { get; set; } = null!;
}

public class VirtualCardPaymentResultDto
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Description { get; set; } = null!;
}
