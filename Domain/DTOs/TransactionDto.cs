namespace SomoniBank.Domain.DTOs;

public class TransferDto
{
    public Guid FromAccountId { get; set; }
    public string ToAccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class DepositMoneyDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class WithdrawMoneyDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class TransactionGetDto
{
    public Guid Id { get; set; }
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public string? FromAccountNumber { get; set; }
    public string? ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}