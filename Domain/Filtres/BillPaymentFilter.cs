namespace SomoniBank.Domain.Filtres;

public class BillPaymentFilter
{
    public Guid? UserId { get; set; }
    public Guid? ProviderId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
