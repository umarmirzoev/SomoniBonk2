namespace SomoniBank.Domain.Filtres;

public class VirtualCardFilter
{
    public Guid? LinkedAccountId { get; set; }
    public string? Status { get; set; }
}
