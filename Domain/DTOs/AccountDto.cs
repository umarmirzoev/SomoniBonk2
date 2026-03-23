namespace SomoniBank.Domain.DTOs;

public class AccountInsertDto
{
    public string Type { get; set; } = null!;
    public string Currency { get; set; } = null!;
}

public class AccountGetDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}