namespace SomoniBank.Domain.DTOs;

public class CardInsertDto
{
    public Guid AccountId { get; set; }
    public string CardHolderName { get; set; } = null!;
}

public class CardGetDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string CardHolderName { get; set; } = null!;
    public string ExpiryDate { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}