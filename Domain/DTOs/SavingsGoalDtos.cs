namespace SomoniBank.Domain.DTOs;

public class SavingsGoalInsertDto
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public string Currency { get; set; } = null!;
    public DateTime Deadline { get; set; }
}

public class SavingsGoalDepositDto
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class SavingsGoalGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string Currency { get; set; } = null!;
    public DateTime Deadline { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
