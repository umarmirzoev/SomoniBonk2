namespace SomoniBank.Domain.DTOs;

public class ExchangeRateDto
{
    public string CurrencyCode { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime RateDate { get; set; }
    public string Source { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class LatestExchangeRatesDto
{
    public DateTime RateDate { get; set; }
    public string Source { get; set; } = null!;
    public List<ExchangeRateDto> Rates { get; set; } = [];
}

public class ExchangeConversionResultDto
{
    public string FromCurrency { get; set; } = null!;
    public string ToCurrency { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public DateTime RateDate { get; set; }
    public string Source { get; set; } = null!;
}
