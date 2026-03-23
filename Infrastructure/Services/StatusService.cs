using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.Enums;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class StatsService : IStatsService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StatsService> _logger;

    public StatsService(AppDbContext db, ILogger<StatsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<object>> GetGeneralStatsAsync()
    {
        try
        {
            var stats = new
            {
                TotalUsers = await _db.Users.CountAsync(),
                ActiveUsers = await _db.Users.CountAsync(x => x.IsActive),
                TotalAccounts = await _db.Accounts.CountAsync(),
                ActiveAccounts = await _db.Accounts.CountAsync(x => x.IsActive),
                TotalCards = await _db.Cards.CountAsync(),
                TotalTransactions = await _db.Transactions.CountAsync(),
                TotalDeposits = await _db.Deposits.CountAsync(x => x.Status == DepositStatus.Active),
                TotalLoans = await _db.Loans.CountAsync(x => x.Status == LoanStatus.Active),
                PendingLoans = await _db.Loans.CountAsync(x => x.Status == LoanStatus.Pending)
            };
            return new Response<object>(HttpStatusCode.OK, "Успешно", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetGeneralStats failed");
            return new Response<object>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<object>> GetTransactionStatsAsync()
    {
        try
        {
            var stats = new
            {
                TotalTransactions = await _db.Transactions.CountAsync(),
                TotalTransfers = await _db.Transactions.CountAsync(x => x.Type == TransactionType.Transfer),
                TotalDeposits = await _db.Transactions.CountAsync(x => x.Type == TransactionType.Deposit),
                TotalWithdrawals = await _db.Transactions.CountAsync(x => x.Type == TransactionType.Withdrawal),
                TotalAmountTransferred = await _db.Transactions
                    .Where(x => x.Type == TransactionType.Transfer && x.Status == TransactionStatus.Completed)
                    .SumAsync(x => x.Amount),
                Last30Days = await _db.Transactions
                    .CountAsync(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            };
            return new Response<object>(HttpStatusCode.OK, "Успешно", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTransactionStats failed");
            return new Response<object>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<object>> GetLoanStatsAsync()
    {
        try
        {
            var stats = new
            {
                TotalLoans = await _db.Loans.CountAsync(),
                ActiveLoans = await _db.Loans.CountAsync(x => x.Status == LoanStatus.Active),
                PendingLoans = await _db.Loans.CountAsync(x => x.Status == LoanStatus.Pending),
                PaidLoans = await _db.Loans.CountAsync(x => x.Status == LoanStatus.Paid),
                RejectedLoans = await _db.Loans.CountAsync(x => x.Status == LoanStatus.Rejected),
                TotalLoanAmount = await _db.Loans
                    .Where(x => x.Status == LoanStatus.Active)
                    .SumAsync(x => x.Amount),
                TotalRemainingAmount = await _db.Loans
                    .Where(x => x.Status == LoanStatus.Active)
                    .SumAsync(x => x.RemainingAmount)
            };
            return new Response<object>(HttpStatusCode.OK, "Успешно", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLoanStats failed");
            return new Response<object>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }
}