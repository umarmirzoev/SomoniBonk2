using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FromAccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public Guid? VirtualCardId { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account? FromAccount { get; set; }
    public Account? ToAccount { get; set; }
    public VirtualCard? VirtualCard { get; set; }
}
