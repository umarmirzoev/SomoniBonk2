using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Filtres;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(AppDbContext db, IFraudDetectionService fraudDetectionService, ILogger<TransactionService> logger)
    {
        _db = db;
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    public async Task<Response<TransactionGetDto>> GetByIdAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Transaction> query = _db.Transactions.AsNoTracking()
                .Include(x => x.FromAccount)
                .Include(x => x.ToAccount);

            if (!isAdmin && requesterUserId.HasValue)
            {
                query = query.Where(x =>
                    (x.FromAccount != null && x.FromAccount.UserId == requesterUserId.Value) ||
                    (x.ToAccount != null && x.ToAccount.UserId == requesterUserId.Value));
            }

            var transaction = await query
                .FirstOrDefaultAsync(x => x.Id == id);
            if (transaction == null)
                return new Response<TransactionGetDto>(HttpStatusCode.NotFound, "Transaction not found");

            return new Response<TransactionGetDto>(HttpStatusCode.OK, "Success", MapToDto(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTransactionById failed");
            return new Response<TransactionGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<TransactionGetDto>> GetAllAsync(TransactionFilter filter, PagedQuery pagedQuery, Guid? requesterUserId = null, bool isAdmin = false)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<Transaction> query = _db.Transactions.AsNoTracking()
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount);

        if (!isAdmin && requesterUserId.HasValue)
        {
            query = query.Where(x =>
                (x.FromAccount != null && x.FromAccount.UserId == requesterUserId.Value) ||
                (x.ToAccount != null && x.ToAccount.UserId == requesterUserId.Value));
        }

        if (filter?.AccountId != null)
            query = query.Where(x => x.FromAccountId == filter.AccountId || x.ToAccountId == filter.AccountId);
        if (!string.IsNullOrEmpty(filter?.Type))
            query = query.Where(x => x.Type == Enum.Parse<TransactionType>(filter.Type, true));
        if (!string.IsNullOrEmpty(filter?.Status))
            query = query.Where(x => x.Status == Enum.Parse<TransactionStatus>(filter.Status, true));
        if (filter?.FromDate != null)
            query = query.Where(x => x.CreatedAt >= filter.FromDate);
        if (filter?.ToDate != null)
            query = query.Where(x => x.CreatedAt <= filter.ToDate);
        if (filter?.MinAmount != null)
            query = query.Where(x => x.Amount >= filter.MinAmount);
        if (filter?.MaxAmount != null)
            query = query.Where(x => x.Amount <= filter.MaxAmount);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TransactionGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> TransferAsync(Guid userId, TransferDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var fromAccount = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (fromAccount == null)
                return new Response<string>(HttpStatusCode.NotFound, "Sender account not found");

            if (!fromAccount.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Sender account is blocked");

            if (fromAccount.Balance < dto.Amount)
                return new Response<string>(HttpStatusCode.BadRequest, "Insufficient funds");

            var toAccount = await _db.Accounts
                .FirstOrDefaultAsync(x => x.AccountNumber == dto.ToAccountNumber);
            if (toAccount == null)
                return new Response<string>(HttpStatusCode.NotFound, "Recipient account not found");

            if (!toAccount.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Recipient account is blocked");

            var fraudCheck = await _fraudDetectionService.EvaluateTransferAsync(userId, fromAccount, dto.Amount, dto.Description);
            if (fraudCheck.IsBlocked)
                return new Response<string>(HttpStatusCode.Forbidden, $"Transfer blocked by fraud monitoring: {fraudCheck.Reason}");

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = dto.Amount,
                Currency = fromAccount.Currency,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Completed,
                Description = dto.Description ?? "Transfer"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new Response<string>(HttpStatusCode.OK, $"Transfer {dto.Amount} completed successfully");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Transfer failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> DepositMoneyAsync(Guid userId, DepositMoneyDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Account is blocked");

            account.Balance += dto.Amount;

            _db.Transactions.Add(new Transaction
            {
                ToAccountId = account.Id,
                Amount = dto.Amount,
                Currency = account.Currency,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = dto.Description ?? "Account top up"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new Response<string>(HttpStatusCode.OK, $"Account topped up by {dto.Amount}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "DepositMoney failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> WithdrawMoneyAsync(Guid userId, WithdrawMoneyDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Account is blocked");

            if (account.Balance < dto.Amount)
                return new Response<string>(HttpStatusCode.BadRequest, "Insufficient funds");

            account.Balance -= dto.Amount;

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = dto.Amount,
                Currency = account.Currency,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Description = dto.Description ?? "Cash withdrawal"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new Response<string>(HttpStatusCode.OK, $"Withdrawn {dto.Amount}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "WithdrawMoney failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> ExchangeCurrencyAsync(Guid userId, CurrencyExchangeDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var fromAccount = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (fromAccount == null)
                return new Response<string>(HttpStatusCode.NotFound, "Sender account not found");

            var toAccount = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.ToAccountId && x.UserId == userId);
            if (toAccount == null)
                return new Response<string>(HttpStatusCode.NotFound, "Recipient account not found");

            if (fromAccount.Currency == toAccount.Currency)
                return new Response<string>(HttpStatusCode.BadRequest, "Accounts use the same currency");

            if (fromAccount.Balance < dto.Amount)
                return new Response<string>(HttpStatusCode.BadRequest, "Insufficient funds");

            var rate = await _db.CurrencyRates
                .FirstOrDefaultAsync(x => x.FromCurrency == fromAccount.Currency && x.ToCurrency == toAccount.Currency);
            if (rate == null)
                return new Response<string>(HttpStatusCode.NotFound, "Exchange rate not found");

            var convertedAmount = dto.Amount * rate.Rate;
            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += convertedAmount;

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = dto.Amount,
                Currency = fromAccount.Currency,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Completed,
                Description = $"Currency exchange: {dto.Amount} {fromAccount.Currency} -> {convertedAmount:F2} {toAccount.Currency}"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new Response<string>(HttpStatusCode.OK, $"Currency exchange completed. Received: {convertedAmount:F2} {toAccount.Currency}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "ExchangeCurrency failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static TransactionGetDto MapToDto(Transaction t) => new()
    {
        Id = t.Id,
        FromAccountId = t.FromAccountId,
        ToAccountId = t.ToAccountId,
        FromAccountNumber = t.FromAccount?.AccountNumber,
        ToAccountNumber = t.ToAccount?.AccountNumber,
        Amount = t.Amount,
        Currency = t.Currency.ToString(),
        Type = t.Type.ToString(),
        Status = t.Status.ToString(),
        Description = t.Description,
        CreatedAt = t.CreatedAt
    };
}
