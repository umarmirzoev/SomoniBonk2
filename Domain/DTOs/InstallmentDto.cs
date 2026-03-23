namespace SomoniBank.Domain.DTOs;

public class InstallmentInsertDto
{
    public Guid AccountId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int TermMonths { get; set; }
    public string Currency { get; set; } = null!;
}

public class InstallmentGetDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal MonthlyPayment { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int TermMonths { get; set; }
    public int PaidMonths { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime NextPaymentDate { get; set; }
}

public class InstallmentPaymentDto
{
    public Guid InstallmentId { get; set; }
    public Guid FromAccountId { get; set; }
}