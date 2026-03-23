namespace SomoniBank.Domain.DTOs;

public class DepositInsertDto
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public string Currency { get; set; } = null!;
}

public class DepositGetDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal ExpectedProfit { get; set; }
}