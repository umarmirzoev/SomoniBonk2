using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class Deposit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal InterestRate { get; set; }
    public Currency Currency { get; set; }
    public DepositStatus Status { get; set; } = DepositStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}