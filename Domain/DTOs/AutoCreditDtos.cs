namespace SomoniBank.Domain.DTOs;

public class AutoCreditInsertDto
{
    public Guid AccountId { get; set; }
    public string CarBrand { get; set; } = null!;
    public string CarModel { get; set; } = null!;
    public int CarYear { get; set; }
    public decimal CarPrice { get; set; }
    public decimal DownPayment { get; set; }
    public int TermMonths { get; set; }
    public string Currency { get; set; } = null!;
}

public class AutoCreditGetDto
{
    public Guid Id { get; set; }
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
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AutoCreditPaymentDto
{
    public Guid AutoCreditId { get; set; }
    public Guid FromAccountId { get; set; }
}
