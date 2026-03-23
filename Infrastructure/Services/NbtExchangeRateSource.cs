using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.Infrastructure.Services;

public sealed class NbtExchangeRateSource(HttpClient httpClient, ILogger<NbtExchangeRateSource> logger) : INbtExchangeRateSource
{
    private const string HtmlSourceUrl = "https://www.nbt.tj/en/kurs/kurs.php";
    private const string XmlSourceUrl = "https://www.nbt.tj/en/kurs/export_xml.php?export=xmlout";
    private static readonly Regex[] RateDateRegexes =
    [
        new Regex(@"as\s+of\s+(?<date>\d{2}[./]\d{2}[./]\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"official\s+exchange\s+rates.*?(?<date>\d{2}[./]\d{2}[./]\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(?<date>\d{2}[./]\d{2}[./]\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];
    private static readonly Regex RowRegex = new(@"<tr[^>]*>(?<content>.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CellRegex = new(@"<t[dh][^>]*>(?<content>.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TagRegex = new(@"<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Dictionary<string, string> NumericCodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["840"] = "USD",
        ["978"] = "EUR",
        ["810"] = "RUB"
    };
    private static readonly Dictionary<string, string> CurrencyNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "USD",
        ["US DOLLAR"] = "USD",
        ["UNITED STATES DOLLAR"] = "USD",
        ["ДОЛЛАР США"] = "USD",
        ["EUR"] = "EUR",
        ["EURO"] = "EUR",
        ["ЕВРО"] = "EUR",
        ["RUB"] = "RUB",
        ["RUSSIAN RUBLE"] = "RUB",
        ["RUSSIAN ROUBLE"] = "RUB",
        ["РОССИЙСКИЙ РУБЛЬ"] = "RUB",
        ["RUR"] = "RUB"
    };
    private static readonly string[] SupportedCurrencies = ["USD", "EUR", "RUB"];

    public async Task<NbtExchangeRateSnapshot> FetchLatestRatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var xml = await DownloadContentAsync(XmlSourceUrl, cancellationToken);
            return ParseXmlSnapshot(xml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "NBT XML exchange-rate feed could not be parsed. Falling back to HTML page.");
        }

        var html = await DownloadContentAsync(HtmlSourceUrl, cancellationToken);
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
            Source = HtmlSourceUrl,
            Rates = items.Select(x => new NbtExchangeRateItem
            {
                CurrencyCode = x.Key,
                Rate = x.Value
            }).ToArray()
        };
    }

    private async Task<string> DownloadContentAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept-Language", "en");
        request.Headers.TryAddWithoutValidation("User-Agent", "SomoniBank/1.0");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private NbtExchangeRateSnapshot ParseXmlSnapshot(string xml)
    {
        var document = XDocument.Parse(xml);
        var rateDate = ParseRateDateFromXml(document) ?? DateTime.UtcNow.AddHours(5).Date;
        var items = ParseRatesFromXml(document);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("NBT XML exchange-rate feed did not contain any supported rates.");
        }

        items["TJS"] = 1m;

        return new NbtExchangeRateSnapshot
        {
            RateDate = rateDate,
            Source = XmlSourceUrl,
            Rates = items.Select(x => new NbtExchangeRateItem
            {
                CurrencyCode = x.Key,
                Rate = x.Value
            }).ToArray()
        };
    }

    private static DateTime ParseRateDate(string html)
    {
        var normalizedText = CleanCellValue(html);

        foreach (var regex in RateDateRegexes)
        {
            var match = regex.Match(normalizedText);
            if (!match.Success)
            {
                continue;
            }

            var rawDate = match.Groups["date"].Value;
            var supportedFormats = new[] { "dd.MM.yyyy", "dd/MM/yyyy" };

            if (DateTime.TryParseExact(rawDate, supportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        throw new InvalidOperationException("NBT exchange rates page did not contain a parsable rate date.");
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

        if (result.Count == 0)
        {
            result = ParseRatesFromText(CleanCellValue(html));
        }

        EnsureRequiredCurrencies(result, "NBT exchange rates page");
        return result;
    }

    private Dictionary<string, decimal> ParseRatesFromXml(XDocument document)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var element in document.Descendants().Where(x => x.HasElements))
        {
            var fields = element.Elements()
                .GroupBy(child => NormalizeFieldName(child.Name.LocalName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => CleanCellValue(group.Last().Value),
                    StringComparer.OrdinalIgnoreCase);

            var currencyCode = ResolveCurrencyCode(fields);
            if (currencyCode == null)
            {
                continue;
            }

            if (!TryGetUnit(fields, out var unit) || unit <= 0)
            {
                unit = 1m;
            }

            if (!TryGetRate(fields, out var rate))
            {
                continue;
            }

            result[currencyCode] = decimal.Round(rate / unit, 6, MidpointRounding.AwayFromZero);
        }

        EnsureRequiredCurrencies(result, "NBT XML exchange-rate feed");
        return result;
    }

    private Dictionary<string, decimal> ParseRatesFromText(string text)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var i = 0; i < tokens.Length; i++)
        {
            var currencyCode = ResolveCurrencyCode(tokens[i]);
            if (currencyCode == null || result.ContainsKey(currencyCode))
            {
                continue;
            }

            var numericWindow = tokens
                .Skip(i + 1)
                .Take(8)
                .Select(CleanCellValue)
                .Where(token => token.Any(char.IsDigit))
                .ToArray();

            if (numericWindow.Length == 0)
            {
                continue;
            }

            decimal unit = 1m;
            decimal rate;

            if (numericWindow.Length >= 2
                && TryParseDecimal(numericWindow[0], out var parsedUnit)
                && parsedUnit > 0
                && TryParseDecimal(numericWindow[1], out var parsedRate))
            {
                unit = parsedUnit;
                rate = parsedRate;
            }
            else if (TryParseDecimal(numericWindow[0], out var fallbackRate))
            {
                rate = fallbackRate;
            }
            else
            {
                continue;
            }

            result[currencyCode] = decimal.Round(rate / unit, 6, MidpointRounding.AwayFromZero);
        }

        return result;
    }

    private static DateTime? ParseRateDateFromXml(XDocument document)
    {
        var dateCandidates = document.Root?
            .DescendantsAndSelf()
            .Attributes()
            .Where(attribute => attribute.Name.LocalName.Contains("date", StringComparison.OrdinalIgnoreCase))
            .Select(attribute => attribute.Value)
            .Concat(document.Descendants()
                .Where(element => element.Name.LocalName.Contains("date", StringComparison.OrdinalIgnoreCase))
                .Select(element => element.Value))
            ?? Enumerable.Empty<string>();

        foreach (var candidate in dateCandidates)
        {
            var cleanValue = CleanCellValue(candidate);
            var formats = new[] { "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "yyyyMMdd" };
            if (DateTime.TryParseExact(cleanValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
            }
        }

        return null;
    }

    private static string NormalizeFieldName(string value)
        => Regex.Replace(value, @"[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();

    private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> fields)
    {
        foreach (var key in new[] { "charcode", "alphabeticcode", "currencycode", "code" })
        {
            if (fields.TryGetValue(key, out var rawCode))
            {
                var resolved = ResolveCurrencyCode(rawCode);
                if (resolved != null)
                {
                    return resolved;
                }
            }
        }

        foreach (var key in new[] { "numcode", "numericcode", "digitalcode", "num" })
        {
            if (fields.TryGetValue(key, out var numericCode)
                && NumericCodeMap.TryGetValue(CleanCellValue(numericCode), out var resolved))
            {
                return resolved;
            }
        }

        foreach (var key in new[] { "name", "currencyname", "fullname", "title" })
        {
            if (fields.TryGetValue(key, out var name))
            {
                var resolved = ResolveCurrencyCode(name);
                if (resolved != null)
                {
                    return resolved;
                }
            }
        }

        return null;
    }

    private static string? ResolveCurrencyCode(string rawValue)
    {
        var normalized = CleanCellValue(rawValue).Trim().ToUpperInvariant();
        if (CurrencyNameMap.TryGetValue(normalized, out var mapped))
        {
            return mapped;
        }

        if (NumericCodeMap.TryGetValue(normalized, out var numericMapped))
        {
            return numericMapped;
        }

        return SupportedCurrencies.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : null;
    }

    private static bool TryGetUnit(IReadOnlyDictionary<string, string> fields, out decimal unit)
    {
        foreach (var key in new[] { "nominal", "unit", "units", "count", "quantity" })
        {
            if (fields.TryGetValue(key, out var value) && TryParseDecimal(value, out unit))
            {
                return true;
            }
        }

        unit = 0;
        return false;
    }

    private static bool TryGetRate(IReadOnlyDictionary<string, string> fields, out decimal rate)
    {
        foreach (var key in new[] { "value", "rate", "officialrate", "course", "sell", "mid" })
        {
            if (fields.TryGetValue(key, out var value) && TryParseDecimal(value, out rate))
            {
                return true;
            }
        }

        rate = 0;
        return false;
    }

    private static void EnsureRequiredCurrencies(IReadOnlyDictionary<string, decimal> result, string sourceName)
    {
        foreach (var requiredCurrency in SupportedCurrencies)
        {
            if (!result.ContainsKey(requiredCurrency))
            {
                throw new InvalidOperationException($"{sourceName} did not contain required currency {requiredCurrency}.");
            }
        }
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
