using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class AutoCredit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string CarBrand { get; set; } = null!;
    public string CarModel { get; set; } = null!;
    public int CarYear { get; set; }
    public decimal CarPrice { get; set; }
    public decimal DownPayment { get; set; }
    public decimal LoanAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal RemainingAmount { get; set; }
    public int TermMonths { get; set; }
    public Currency Currency { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Pending;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
