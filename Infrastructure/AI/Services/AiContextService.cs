using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Application.AI.DTOs;
using SomoniBank.Application.AI.Interfaces;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;

namespace SomoniBank.Infrastructure.AI.Services;

public class AiContextService : IAiContextService
{
    private static readonly Currency[] PreferredCurrencies = [Currency.USD, Currency.EUR, Currency.RUB];

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AiContextService> _logger;

    public AiContextService(
        AppDbContext db,
        ICurrentUserService currentUserService,
        ILogger<AiContextService> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AiContextDto> BuildContextAsync(CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            _logger.LogWarning("AI context requested without a valid authenticated user id.");
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("AI context requested for unknown user id {UserId}.", userId);
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var accounts = await _db.Accounts.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var accountIds = accounts.Select(x => x.Id).ToList();

        var transactions = await _db.Transactions.AsNoTracking()
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount)
            .Where(x =>
                (x.FromAccountId.HasValue && accountIds.Contains(x.FromAccountId.Value)) ||
                (x.ToAccountId.HasValue && accountIds.Contains(x.ToAccountId.Value)))
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var exchangeRates = await LoadExchangeRatesAsync(cancellationToken);

        return new AiContextDto
        {
            UserFullName = $"{user.FirstName} {user.LastName}".Trim(),
            TotalBalance = accounts.Sum(x => x.Balance),
            Accounts = accounts.Select(FormatAccount).ToList(),
            RecentTransactions = transactions.Select(x => FormatTransaction(x, accountIds)).ToList(),
            ExchangeRates = exchangeRates,
            CurrencySummary = BuildCurrencySummary(accounts)
        };
    }

    private async Task<List<string>> LoadExchangeRatesAsync(CancellationToken cancellationToken)
    {
        var latestRates = await _db.CurrencyRates.AsNoTracking()
            .Where(x =>
                (x.FromCurrency == Currency.TJS && PreferredCurrencies.Contains(x.ToCurrency)) ||
                (x.ToCurrency == Currency.TJS && PreferredCurrencies.Contains(x.FromCurrency)))
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return latestRates
            .GroupBy(x => $"{x.FromCurrency}-{x.ToCurrency}")
            .Select(g => g.First())
            .OrderBy(x => x.FromCurrency == Currency.TJS ? 0 : 1)
            .ThenBy(x => x.ToCurrency.ToString())
            .Select(x => $"{x.FromCurrency}/{x.ToCurrency}: {x.Rate.ToString("0.####", CultureInfo.InvariantCulture)}")
            .ToList();
    }

    private static string FormatAccount(Account account)
    {
        var lastDigits = account.AccountNumber.Length > 4
            ? account.AccountNumber[^4..]
            : account.AccountNumber;

        var status = account.IsActive ? "active" : "inactive";
        return $"{account.Type} account ****{lastDigits}, {account.Currency}, balance {account.Balance.ToString("0.##", CultureInfo.InvariantCulture)}, {status}";
    }

    private static string FormatTransaction(Transaction transaction, IReadOnlyCollection<Guid> ownAccountIds)
    {
        var direction = ResolveDirection(transaction, ownAccountIds);
        var description = string.IsNullOrWhiteSpace(transaction.Description) ? "No description" : transaction.Description.Trim();
        var status = transaction.Status.ToString();
        var date = transaction.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return $"{date} | {transaction.Type} | {direction} | {transaction.Amount.ToString("0.##", CultureInfo.InvariantCulture)} {transaction.Currency} | {description} | {status}";
    }

    private static string ResolveDirection(Transaction transaction, IReadOnlyCollection<Guid> ownAccountIds)
    {
        var isOutgoing = transaction.FromAccountId.HasValue && ownAccountIds.Contains(transaction.FromAccountId.Value);
        var isIncoming = transaction.ToAccountId.HasValue && ownAccountIds.Contains(transaction.ToAccountId.Value);

        return (isOutgoing, isIncoming) switch
        {
            (true, true) => "internal",
            (true, false) => "outgoing",
            (false, true) => "incoming",
            _ => "related"
        };
    }

    private static string BuildCurrencySummary(IEnumerable<Account> accounts)
    {
        var parts = accounts
            .GroupBy(x => x.Currency)
            .OrderBy(x => x.Key.ToString())
            .Select(group => $"{group.Key}: {group.Sum(x => x.Balance).ToString("0.##", CultureInfo.InvariantCulture)}")
            .ToList();

        return parts.Count > 0
            ? string.Join("; ", parts)
            : "No account balances available";
    }
}
