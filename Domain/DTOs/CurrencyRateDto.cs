namespace SomoniBank.Domain.DTOs;

public class CurrencyRateInsertDto
{
    public string FromCurrency { get; set; } = null!;
    public string ToCurrency { get; set; } = null!;
    public decimal Rate { get; set; }
}

public class CurrencyRateGetDto
{
    public Guid Id { get; set; }
    public string FromCurrency { get; set; } = null!;
    public string ToCurrency { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CurrencyConvertDto
{
    public string FromCurrency { get; set; } = null!;
    public string ToCurrency { get; set; } = null!;
    public decimal Amount { get; set; }
}