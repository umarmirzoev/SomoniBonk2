namespace SomoniBank.Domain.DTOs;

public class InternationalTransferInsertDto
{
    public Guid FromAccountId { get; set; }
    public string RecipientName { get; set; } = null!;
    public string RecipientBank { get; set; } = null!;
    public string RecipientAccount { get; set; } = null!;
    public string Country { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
}

public class InternationalTransferGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FromAccountId { get; set; }
    public string FromAccountNumber { get; set; } = null!;
    public string RecipientName { get; set; } = null!;
    public string RecipientBank { get; set; } = null!;
    public string RecipientAccount { get; set; } = null!;
    public string Country { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; }
    public decimal AmountInTJS { get; set; }
    public decimal Fee { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
