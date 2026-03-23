using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(AppDbContext db, ILogger<AnalyticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<IncomeVsExpenseDto>> GetIncomeVsExpenseAsync(Guid userId, AnalyticsPeriodDto period)
    {
        try
        {
            period ??= new AnalyticsPeriodDto();
            var accountIds = GetOwnedAccountIdsQuery(userId, period.AccountId);

            var incomeQuery = ApplyTransactionPeriodFilter(_db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed && x.ToAccountId != null && accountIds.Contains(x.ToAccountId.Value)), period);

            var expenseQuery = ApplyTransactionPeriodFilter(_db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed && x.FromAccountId != null && accountIds.Contains(x.FromAccountId.Value)), period);

            var totalIncome = await incomeQuery.SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var totalExpense = await expenseQuery.SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var netBalance = totalIncome - totalExpense;
            var savingsRate = totalIncome == 0 ? 0 : Math.Round(netBalance / totalIncome * 100, 2);

            return new Response<IncomeVsExpenseDto>(HttpStatusCode.OK, "Success", new IncomeVsExpenseDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetBalance = netBalance,
                SavingsRate = savingsRate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get income vs expense failed");
            return new Response<IncomeVsExpenseDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<List<SpendingByCategoryDto>>> GetSpendingByCategoryAsync(Guid userId, AnalyticsPeriodDto period)
    {
        try
        {
            period ??= new AnalyticsPeriodDto();
            var accountIds = GetOwnedAccountIdsQuery(userId, period.AccountId);

            var expenseQuery = ApplyTransactionPeriodFilter(_db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed && x.FromAccountId != null && accountIds.Contains(x.FromAccountId.Value)), period);

            var grouped = await expenseQuery
                .GroupBy(x => x.Type)
                .Select(x => new
                {
                    Category = x.Key.ToString(),
                    TotalAmount = x.Sum(y => y.Amount),
                    TransactionCount = x.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            var totalSpending = grouped.Sum(x => x.TotalAmount);
            var result = grouped.Select(x => new SpendingByCategoryDto
            {
                Category = x.Category,
                TotalAmount = x.TotalAmount,
                TransactionCount = x.TransactionCount,
                Percentage = totalSpending == 0 ? 0 : Math.Round(x.TotalAmount / totalSpending * 100, 2)
            }).ToList();

            return new Response<List<SpendingByCategoryDto>>(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get spending by category failed");
            return new Response<List<SpendingByCategoryDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<List<MonthlyStatsDto>>> GetMonthlyStatsAsync(Guid userId, int year)
    {
        try
        {
            var accountIds = GetOwnedAccountIdsQuery(userId, null);
            var incomeQuery = _db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed
                            && x.CreatedAt.Year == year
                            && x.ToAccountId != null
                            && accountIds.Contains(x.ToAccountId.Value));

            var expenseQuery = _db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed
                            && x.CreatedAt.Year == year
                            && x.FromAccountId != null
                            && accountIds.Contains(x.FromAccountId.Value));

            var incomeByMonth = await incomeQuery
                .GroupBy(x => x.CreatedAt.Month)
                .Select(x => new { Month = x.Key, Total = x.Sum(y => y.Amount) })
                .ToDictionaryAsync(x => x.Month, x => x.Total);

            var expenseByMonth = await expenseQuery
                .GroupBy(x => x.CreatedAt.Month)
                .Select(x => new { Month = x.Key, Total = x.Sum(y => y.Amount) })
                .ToDictionaryAsync(x => x.Month, x => x.Total);

            var result = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var totalIncome = incomeByMonth.GetValueOrDefault(month);
                    var totalExpense = expenseByMonth.GetValueOrDefault(month);
                    return new MonthlyStatsDto
                    {
                        Year = year,
                        Month = month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                        TotalIncome = totalIncome,
                        TotalExpense = totalExpense,
                        NetBalance = totalIncome - totalExpense
                    };
                })
                .ToList();

            return new Response<List<MonthlyStatsDto>>(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get monthly stats failed");
            return new Response<List<MonthlyStatsDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<List<TopRecipientDto>>> GetTopRecipientsAsync(Guid userId, AnalyticsPeriodDto period)
    {
        try
        {
            period ??= new AnalyticsPeriodDto();
            var accountIds = GetOwnedAccountIdsQuery(userId, period.AccountId);

            var query = ApplyTransactionPeriodFilter(_db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed
                            && x.Type == TransactionType.Transfer
                            && x.FromAccountId != null
                            && x.ToAccountId != null
                            && accountIds.Contains(x.FromAccountId.Value)), period);

            var recipients = await query
                .Join(_db.Accounts.AsNoTracking(),
                    transaction => transaction.ToAccountId,
                    account => account.Id,
                    (transaction, account) => new { transaction, account.AccountNumber })
                .GroupBy(x => x.AccountNumber)
                .Select(x => new TopRecipientDto
                {
                    AccountNumber = x.Key,
                    TransferCount = x.Count(),
                    TotalAmount = x.Sum(y => y.transaction.Amount)
                })
                .OrderByDescending(x => x.TransferCount)
                .ThenByDescending(x => x.TotalAmount)
                .Take(5)
                .ToListAsync();

            return new Response<List<TopRecipientDto>>(HttpStatusCode.OK, "Success", recipients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get top recipients failed");
            return new Response<List<TopRecipientDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<object>> GetDailySpendingAsync(Guid userId, AnalyticsPeriodDto period)
    {
        try
        {
            period ??= new AnalyticsPeriodDto();
            var accountIds = GetOwnedAccountIdsQuery(userId, period.AccountId);

            var query = ApplyTransactionPeriodFilter(_db.Transactions.AsNoTracking()
                .Where(x => x.Status == TransactionStatus.Completed
                            && x.FromAccountId != null
                            && accountIds.Contains(x.FromAccountId.Value)), period);

            var dailySpending = await query
                .GroupBy(x => x.CreatedAt.Date)
                .Select(x => new
                {
                    Date = x.Key,
                    Amount = x.Sum(y => y.Amount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new Response<object>(HttpStatusCode.OK, "Success", dailySpending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get daily spending failed");
            return new Response<object>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private IQueryable<Guid> GetOwnedAccountIdsQuery(Guid userId, Guid? accountId)
    {
        var query = _db.Accounts.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Id);

        if (accountId.HasValue)
            query = query.Where(x => x == accountId.Value);

        return query;
    }

    private static IQueryable<Domain.Models.Transaction> ApplyTransactionPeriodFilter(IQueryable<Domain.Models.Transaction> query, AnalyticsPeriodDto period)
    {
        if (period.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= period.FromDate.Value);
        if (period.ToDate.HasValue)
            query = query.Where(x => x.CreatedAt <= period.ToDate.Value);

        return query;
    }
}
