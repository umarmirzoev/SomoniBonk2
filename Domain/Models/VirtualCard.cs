using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class VirtualCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LinkedAccountId { get; set; }
    public string CardHolderName { get; set; } = null!;
    public string MaskedCardNumber { get; set; } = null!;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string CvvHash { get; set; } = null!;
    public decimal DailyLimit { get; set; }
    public decimal MonthlyLimit { get; set; }
    public bool IsSingleUse { get; set; }
    public VirtualCardStatus Status { get; set; } = VirtualCardStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Account LinkedAccount { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
