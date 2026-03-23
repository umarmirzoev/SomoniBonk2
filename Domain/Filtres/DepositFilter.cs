namespace SomoniBank.Domain.Filtres;

public class DepositFilter
{
    public Guid? AccountId { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
}