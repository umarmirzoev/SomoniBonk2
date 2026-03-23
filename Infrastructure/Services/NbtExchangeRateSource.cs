using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.Infrastructure.Services;

public sealed class NbtExchangeRateSource(HttpClient httpClient, ILogger<NbtExchangeRateSource> logger) : INbtExchangeRateSource
{
    private const string SourceUrl = "https://www.nbt.tj/en/kurs/kurs.php";
    private static readonly Regex RateDateRegex = new(@"as of\s+(?<date>\d{2}\.\d{2}\.\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RowRegex = new(@"<tr[^>]*>(?<content>.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CellRegex = new(@"<t[dh][^>]*>(?<content>.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TagRegex = new(@"<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Dictionary<string, string> NumericCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["840"] = "USD",
        ["978"] = "EUR",
        ["810"] = "RUB"
    };

    public async Task<NbtExchangeRateSnapshot> FetchLatestRatesAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, SourceUrl);
        request.Headers.TryAddWithoutValidation("Accept-Language", "en");
        request.Headers.TryAddWithoutValidation("User-Agent", "SomoniBank/1.0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var rateDate = ParseRateDate(html);
        var items = ParseRates(html);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("NBT exchange rates page did not contain any supported rates.");
        }

        items["TJS"] = 1m;

        return new NbtExchangeRateSnapshot
        {
            RateDate = rateDate,
            Source = SourceUrl,
            Rates = items.Select(x => new NbtExchangeRateItem
            {
                CurrencyCode = x.Key,
                Rate = x.Value
            }).ToArray()
        };
    }

    private static DateTime ParseRateDate(string html)
    {
        var match = RateDateRegex.Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("NBT exchange rates page did not contain a parsable rate date.");
        }

        return DateTime.SpecifyKind(
            DateTime.ParseExact(match.Groups["date"].Value, "dd.MM.yyyy", CultureInfo.InvariantCulture),
            DateTimeKind.Utc);
    }

    private Dictionary<string, decimal> ParseRates(string html)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (Match rowMatch in RowRegex.Matches(html))
        {
            var rawCells = CellRegex.Matches(rowMatch.Groups["content"].Value)
                .Select(match => CleanCellValue(match.Groups["content"].Value))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            if (rawCells.Length < 5)
            {
                continue;
            }

            var numericCode = rawCells[1];
            if (!NumericCodeMap.TryGetValue(numericCode, out var currencyCode))
            {
                continue;
            }

            if (!TryParseDecimal(rawCells[2], out var unit) || unit <= 0)
            {
                logger.LogWarning("Skipping NBT rate row for {CurrencyCode}: invalid unit value {Unit}", currencyCode, rawCells[2]);
                continue;
            }

            if (!TryParseDecimal(rawCells[4], out var rate))
            {
                logger.LogWarning("Skipping NBT rate row for {CurrencyCode}: invalid rate value {Rate}", currencyCode, rawCells[4]);
                continue;
            }

            result[currencyCode] = decimal.Round(rate / unit, 6, MidpointRounding.AwayFromZero);
        }

        foreach (var requiredCurrency in new[] { "USD", "EUR", "RUB" })
        {
            if (!result.ContainsKey(requiredCurrency))
            {
                throw new InvalidOperationException($"NBT exchange rates page did not contain required currency {requiredCurrency}.");
            }
        }

        return result;
    }

    private static string CleanCellValue(string value)
    {
        var withoutTags = TagRegex.Replace(value, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var normalized = value.Replace(" ", string.Empty).Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
