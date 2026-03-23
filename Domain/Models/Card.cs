using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string CardHolderName { get; set; } = null!;
    public string ExpiryDate { get; set; } = null!;
    public string Cvv { get; set; } = null!;
    public CardStatus Status { get; set; } = CardStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}