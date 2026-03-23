namespace SomoniBank.Infrastructure.Services;

public sealed class NbtExchangeRateSnapshot
{
    public DateTime RateDate { get; init; }
    public string Source { get; init; } = null!;
    public IReadOnlyCollection<NbtExchangeRateItem> Rates { get; init; } = [];
}

public sealed class NbtExchangeRateItem
{
    public string CurrencyCode { get; init; } = null!;
    public decimal Rate { get; init; }
}
