using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class CurrencyRate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Currency FromCurrency { get; set; }
    public Currency ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}