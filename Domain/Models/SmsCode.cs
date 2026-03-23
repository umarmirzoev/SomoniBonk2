namespace SomoniBank.Domain.Models;

public class SmsCode
{
    public int Id { get; set; }
    public string Phone { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
