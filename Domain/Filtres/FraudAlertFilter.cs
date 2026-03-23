namespace SomoniBank.Domain.Filtres;

public class FraudAlertFilter
{
    public Guid? UserId { get; set; }
    public string? Status { get; set; }
    public string? RiskLevel { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
