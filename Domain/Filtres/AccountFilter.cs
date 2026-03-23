namespace SomoniBank.Domain.Filtres;

public class AccountFilter
{
    public Guid? UserId { get; set; }
    public string? Type { get; set; }
    public string? Currency { get; set; }
    public bool? IsActive { get; set; }
}