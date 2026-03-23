using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class Installment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal RemainingAmount { get; set; }
    public int TermMonths { get; set; }
    public int PaidMonths { get; set; } = 0;
    public Currency Currency { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Active;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime NextPaymentDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
}