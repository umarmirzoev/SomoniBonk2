using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class Beneficiary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string? AccountNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public BeneficiaryTransferType TransferType { get; set; }
    public string? Nickname { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
