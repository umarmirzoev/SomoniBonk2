namespace SomoniBank.Domain.Models;

public class BillCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ICollection<BillProvider> Providers { get; set; } = new List<BillProvider>();
}
