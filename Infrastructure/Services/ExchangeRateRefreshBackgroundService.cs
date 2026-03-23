using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.Infrastructure.Services;

public sealed class ExchangeRateRefreshBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ExchangeRateRefreshBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshOnceAsync(stoppingToken);
        }
    }

    private async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var exchangeRateService = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
            await exchangeRateService.RefreshRatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exchange-rate background refresh failed.");
        }
    }
}
