namespace SomoniBank.Domain.Models;

public class BillProvider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public BillCategory Category { get; set; } = null!;
    public ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();
}
