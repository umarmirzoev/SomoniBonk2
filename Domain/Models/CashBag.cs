namespace SomoniBank.Domain.Models;

public class Cashback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}