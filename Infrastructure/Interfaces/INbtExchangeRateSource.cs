using SomoniBank.Infrastructure.Services;

namespace SomoniBank.Infrastructure.Interfaces;

public interface INbtExchangeRateSource
{
    Task<NbtExchangeRateSnapshot> FetchLatestRatesAsync(CancellationToken cancellationToken = default);
}
