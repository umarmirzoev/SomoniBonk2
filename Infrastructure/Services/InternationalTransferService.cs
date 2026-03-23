using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class InternationalTransferService : IInternationalTransferService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<InternationalTransferService> _logger;

    public InternationalTransferService(AppDbContext db, INotificationService notificationService, IFraudDetectionService fraudDetectionService, ILogger<InternationalTransferService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    public async Task<Response<InternationalTransferGetDto>> CreateAsync(Guid userId, InternationalTransferInsertDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.Amount <= 0)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.BadRequest, "Amount must be greater than zero");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<InternationalTransferGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (account == null)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.BadRequest, "Account is inactive");

            if (account.Currency != currency)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.BadRequest, "Transfer currency must match account currency");

            var fee = Math.Max(5m, Math.Round(dto.Amount * 0.015m, 2));
            var totalDebit = dto.Amount + fee;
            if (account.Balance < totalDebit)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.BadRequest, "Insufficient funds");

            var fraudCheck = await _fraudDetectionService.EvaluateInternationalTransferAsync(userId, account, dto.Amount, dto.Country);
            if (fraudCheck.IsBlocked)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.Forbidden, $"International transfer blocked by fraud monitoring: {fraudCheck.Reason}");

            var exchangeRate = await GetRateToTjsAsync(currency);
            if (exchangeRate <= 0)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.BadRequest, "Exchange rate to TJS is not configured");

            var transfer = new InternationalTransfer
            {
                UserId = userId,
                FromAccountId = account.Id,
                RecipientName = dto.RecipientName,
                RecipientBank = dto.RecipientBank,
                RecipientAccount = dto.RecipientAccount,
                Country = dto.Country,
                Amount = dto.Amount,
                Currency = currency,
                ExchangeRate = exchangeRate,
                AmountInTJS = Math.Round(dto.Amount * exchangeRate, 2),
                Fee = fee,
                Status = InternationalTransferStatus.Completed
            };

            account.Balance -= totalDebit;
            _db.InternationalTransfers.Add(transfer);
            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = totalDebit,
                Currency = currency,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Description = $"International transfer to {dto.RecipientName} ({dto.Country})"
            });

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "International transfer", $"Transfer to {dto.RecipientName} for {dto.Amount} {currency} completed. Fee: {fee} {currency}.", "InternationalTransfer");
            await dbTransaction.CommitAsync();

            return new Response<InternationalTransferGetDto>(HttpStatusCode.OK, "International transfer created successfully", MapToDto(transfer, account.AccountNumber));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Create international transfer failed");
            return new Response<InternationalTransferGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<InternationalTransferGetDto>> GetByIdAsync(Guid userId, Guid id, bool isAdmin = false)
    {
        try
        {
            IQueryable<InternationalTransfer> query = _db.InternationalTransfers.AsNoTracking()
                .Include(x => x.FromAccount)
                .Where(x => x.Id == id);

            if (!isAdmin)
                query = query.Where(x => x.UserId == userId);

            var transfer = await query.FirstOrDefaultAsync();
            if (transfer == null)
                return new Response<InternationalTransferGetDto>(HttpStatusCode.NotFound, "Transfer not found");

            return new Response<InternationalTransferGetDto>(HttpStatusCode.OK, "Success", MapToDto(transfer, transfer.FromAccount.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get international transfer by id failed");
            return new Response<InternationalTransferGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<InternationalTransferGetDto>> GetMyTransfersAsync(Guid userId, PagedQuery pagedQuery)
    {
        return await GetPagedTransfersAsync(_db.InternationalTransfers.AsNoTracking()
            .Include(x => x.FromAccount)
            .Where(x => x.UserId == userId), pagedQuery);
    }

    public async Task<PagedResult<InternationalTransferGetDto>> GetAllAsync(PagedQuery pagedQuery)
    {
        return await GetPagedTransfersAsync(_db.InternationalTransfers.AsNoTracking()
            .Include(x => x.FromAccount), pagedQuery);
    }

    private async Task<PagedResult<InternationalTransferGetDto>> GetPagedTransfersAsync(IQueryable<InternationalTransfer> query, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<InternationalTransferGetDto>
        {
            Items = items.Select(x => MapToDto(x, x.FromAccount.AccountNumber)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private async Task<decimal> GetRateToTjsAsync(Currency fromCurrency)
    {
        if (fromCurrency == Currency.TJS)
            return 1m;

        var directRate = await _db.CurrencyRates.AsNoTracking()
            .Where(x => x.FromCurrency == fromCurrency && x.ToCurrency == Currency.TJS)
            .Select(x => (decimal?)x.Rate)
            .FirstOrDefaultAsync();
        if (directRate.HasValue)
            return directRate.Value;

        var inverseRate = await _db.CurrencyRates.AsNoTracking()
            .Where(x => x.FromCurrency == Currency.TJS && x.ToCurrency == fromCurrency)
            .Select(x => (decimal?)x.Rate)
            .FirstOrDefaultAsync();

        return inverseRate.HasValue && inverseRate.Value > 0 ? Math.Round(1 / inverseRate.Value, 6) : 0m;
    }

    private static InternationalTransferGetDto MapToDto(InternationalTransfer transfer, string accountNumber) => new()
    {
        Id = transfer.Id,
        UserId = transfer.UserId,
        FromAccountId = transfer.FromAccountId,
        FromAccountNumber = accountNumber,
        RecipientName = transfer.RecipientName,
        RecipientBank = transfer.RecipientBank,
        RecipientAccount = transfer.RecipientAccount,
        Country = transfer.Country,
        Amount = transfer.Amount,
        Currency = transfer.Currency.ToString(),
        ExchangeRate = transfer.ExchangeRate,
        AmountInTJS = transfer.AmountInTJS,
        Fee = transfer.Fee,
        Status = transfer.Status.ToString(),
        CreatedAt = transfer.CreatedAt
    };
}
