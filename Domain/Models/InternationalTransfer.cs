using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class InternationalTransfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid FromAccountId { get; set; }
    public string RecipientName { get; set; } = null!;
    public string RecipientBank { get; set; } = null!;
    public string RecipientAccount { get; set; } = null!;
    public string Country { get; set; } = null!;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal AmountInTJS { get; set; }
    public decimal Fee { get; set; }
    public InternationalTransferStatus Status { get; set; } = InternationalTransferStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Account FromAccount { get; set; } = null!;
}
