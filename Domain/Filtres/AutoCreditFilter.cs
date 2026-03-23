namespace SomoniBank.Domain.Filtres;

public class AutoCreditFilter
{
    public Guid? UserId { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
}
