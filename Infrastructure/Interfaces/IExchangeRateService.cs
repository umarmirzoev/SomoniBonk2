using SomoniBank.Domain.DTOs;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IExchangeRateService
{
    Task RefreshRatesAsync(CancellationToken cancellationToken = default);
    Task<LatestExchangeRatesDto> GetLatestRatesAsync(CancellationToken cancellationToken = default);
    Task<ExchangeConversionResultDto> ConvertAsync(string fromCurrency, string toCurrency, decimal amount, CancellationToken cancellationToken = default);
}
