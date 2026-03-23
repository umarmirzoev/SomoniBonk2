namespace SomoniBank.Domain.DTOs;

public class BeneficiaryInsertDto
{
    public string FullName { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string? AccountNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string TransferType { get; set; } = null!;
    public string? Nickname { get; set; }
}

public class BeneficiaryUpdateDto
{
    public string FullName { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string? AccountNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string TransferType { get; set; } = null!;
    public string? Nickname { get; set; }
}

public class BeneficiaryGetDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string? AccountNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string TransferType { get; set; } = null!;
    public string? Nickname { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
}
