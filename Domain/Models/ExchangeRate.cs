namespace SomoniBank.Domain.Models;

public class ExchangeRate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CurrencyCode { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime RateDate { get; set; }
    public string Source { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
