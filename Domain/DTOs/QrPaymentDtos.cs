namespace SomoniBank.Domain.DTOs;

public class GenerateQrDto
{
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public int ExpiresInMinutes { get; set; } = 15;
}

public class PayByQrDto
{
    public Guid FromAccountId { get; set; }
    public string QrCode { get; set; } = null!;
}

public class QrPaymentGetDto
{
    public Guid Id { get; set; }
    public Guid? FromUserId { get; set; }
    public Guid ToAccountId { get; set; }
    public string ToAccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string QrCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
