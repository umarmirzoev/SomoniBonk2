using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.Infrastructure.Services;

public sealed class ExchangeRateService(
    AppDbContext dbContext,
    INbtExchangeRateSource nbtExchangeRateSource,
    ILogger<ExchangeRateService> logger) : IExchangeRateService
{
    private static readonly string[] SupportedCurrencies = ["TJS", "USD", "EUR", "RUB"];

    public async Task RefreshRatesAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await nbtExchangeRateSource.FetchLatestRatesAsync(cancellationToken);
        var existingCodes = await dbContext.ExchangeRates.AsNoTracking()
            .Where(x => x.RateDate == snapshot.RateDate && SupportedCurrencies.Contains(x.CurrencyCode))
            .Select(x => x.CurrencyCode)
            .ToListAsync(cancellationToken);

        if (SupportedCurrencies.All(code => existingCodes.Contains(code, StringComparer.OrdinalIgnoreCase)))
        {
            return;
        }

        var existingRates = await dbContext.ExchangeRates
            .Where(x => x.RateDate == snapshot.RateDate)
            .ToListAsync(cancellationToken);

        foreach (var item in snapshot.Rates.Where(x => SupportedCurrencies.Contains(x.CurrencyCode, StringComparer.OrdinalIgnoreCase)))
        {
            var existing = existingRates.FirstOrDefault(x => x.CurrencyCode == item.CurrencyCode);
            if (existing is null)
            {
                dbContext.ExchangeRates.Add(new ExchangeRate
                {
                    CurrencyCode = item.CurrencyCode,
                    Rate = item.Rate,
                    RateDate = snapshot.RateDate,
                    Source = snapshot.Source,
                    CreatedAt = DateTime.UtcNow
                });
                continue;
            }

            existing.Rate = item.Rate;
            existing.Source = snapshot.Source;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Stored NBT exchange rates for {RateDate}", snapshot.RateDate);
    }

    public async Task<LatestExchangeRatesDto> GetLatestRatesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRatesAvailableAsync(cancellationToken);

        var latestDate = await dbContext.ExchangeRates.AsNoTracking()
            .MaxAsync(x => (DateTime?)x.RateDate, cancellationToken);

        if (latestDate is null)
        {
            return new LatestExchangeRatesDto
            {
                RateDate = DateTime.UtcNow.AddHours(5).Date,
                Source = "Unavailable",
                Rates = []
            };
        }

        var rates = await dbContext.ExchangeRates.AsNoTracking()
            .Where(x => x.RateDate == latestDate.Value)
            .OrderBy(x => x.CurrencyCode)
            .ToListAsync(cancellationToken);

        if (rates.Count == 0)
        {
            return new LatestExchangeRatesDto
            {
                RateDate = latestDate.Value,
                Source = "Unavailable",
                Rates = []
            };
        }

        return new LatestExchangeRatesDto
        {
            RateDate = latestDate.Value,
            Source = rates.First().Source,
            Rates = rates.Select(MapToDto).ToList()
        };
    }

    public async Task<ExchangeConversionResultDto> ConvertAsync(string fromCurrency, string toCurrency, decimal amount, CancellationToken cancellationToken = default)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be non-negative.");
        }

        var normalizedFrom = NormalizeCurrency(fromCurrency);
        var normalizedTo = NormalizeCurrency(toCurrency);

        var latestRates = await GetLatestRatesAsync(cancellationToken);
        if (latestRates.Rates.Count == 0)
        {
            throw new InvalidOperationException("Exchange rates are unavailable.");
        }

        var rateMap = latestRates.Rates.ToDictionary(x => x.CurrencyCode, x => x.Rate, StringComparer.OrdinalIgnoreCase);
        if ((normalizedFrom != "TJS" && !rateMap.ContainsKey(normalizedFrom))
            || (normalizedTo != "TJS" && !rateMap.ContainsKey(normalizedTo)))
        {
            throw new InvalidOperationException("Required exchange rates are unavailable.");
        }

        decimal convertedAmount;

        if (normalizedFrom == normalizedTo)
        {
            convertedAmount = amount;
        }
        else if (normalizedFrom == "TJS")
        {
            convertedAmount = amount / rateMap[normalizedTo];
        }
        else if (normalizedTo == "TJS")
        {
            convertedAmount = amount * rateMap[normalizedFrom];
        }
        else
        {
            var tjsAmount = amount * rateMap[normalizedFrom];
            convertedAmount = tjsAmount / rateMap[normalizedTo];
        }

        return new ExchangeConversionResultDto
        {
            FromCurrency = normalizedFrom,
            ToCurrency = normalizedTo,
            Amount = amount,
            ConvertedAmount = decimal.Round(convertedAmount, 4, MidpointRounding.AwayFromZero),
            RateDate = latestRates.RateDate,
            Source = latestRates.Source
        };
    }

    private async Task EnsureRatesAvailableAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.ExchangeRates.AnyAsync(cancellationToken))
        {
            await RefreshRatesAsync(cancellationToken);
            return;
        }

        var tajikistanToday = DateTime.UtcNow.AddHours(5).Date;
        var hasTodayRates = await dbContext.ExchangeRates.AsNoTracking()
            .AnyAsync(x => x.RateDate == tajikistanToday, cancellationToken);

        if (!hasTodayRates)
        {
            try
            {
                await RefreshRatesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Refreshing today's exchange rates failed; falling back to latest stored rates.");
            }
        }
    }

    private static ExchangeRateDto MapToDto(ExchangeRate entity) => new()
    {
        CurrencyCode = entity.CurrencyCode,
        Rate = entity.Rate,
        RateDate = entity.RateDate,
        Source = entity.Source,
        CreatedAt = entity.CreatedAt
    };

    private static string NormalizeCurrency(string currencyCode)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (!SupportedCurrencies.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Currency {currencyCode} is not supported.", nameof(currencyCode));
        }

        return normalized;
    }
}
