namespace SomoniBank.Domain.Filtres;

public class CardFilter
{
    public Guid? UserId { get; set; }
    public Guid? AccountId { get; set; }
    public string? Status { get; set; }
}
