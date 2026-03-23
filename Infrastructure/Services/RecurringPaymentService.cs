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

public class RecurringPaymentService : IRecurringPaymentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RecurringPaymentService> _logger;

    public RecurringPaymentService(
        AppDbContext db,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ILogger<RecurringPaymentService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Response<RecurringPaymentGetDto>> CreateAsync(Guid userId, RecurringPaymentInsertDto dto)
    {
        try
        {
            if (!await _db.KycProfiles.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Status == KycStatus.Approved))
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Approved KYC is required for recurring payments");

            if (dto.Amount <= 0)
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Amount must be greater than zero");

            if (!Enum.TryParse<RecurringPaymentCategory>(dto.Category, true, out var category))
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Invalid category");

            if (!Enum.TryParse<RecurringPaymentFrequency>(dto.Frequency, true, out var frequency))
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Invalid frequency");

            if (!Enum.TryParse<Currency>(dto.CurrencyCode, true, out var currency))
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            if (dto.NextExecutionDate <= DateTime.UtcNow)
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Next execution date must be in the future");

            var account = await _db.Accounts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Account is inactive");

            if (account.Currency != currency)
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.BadRequest, "Recurring payment currency must match account currency");

            var recurringPayment = new RecurringPayment
            {
                UserId = userId,
                AccountId = dto.AccountId,
                ProviderName = dto.ProviderName.Trim(),
                Category = category,
                Amount = dto.Amount,
                CurrencyCode = currency,
                Frequency = frequency,
                NextExecutionDate = dto.NextExecutionDate,
                Status = RecurringPaymentStatus.Active,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
            };

            _db.RecurringPayments.Add(recurringPayment);
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Recurring payment created", $"Recurring payment for {recurringPayment.ProviderName} has been created.", "RecurringPayment");
            await _auditLogService.LogAsync(userId, "RecurringPaymentCreated", "", "", true);

            return new Response<RecurringPaymentGetDto>(HttpStatusCode.OK, "Recurring payment created successfully", MapToDto(recurringPayment, account.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create recurring payment failed");
            return new Response<RecurringPaymentGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<RecurringPaymentGetDto>> GetMyAsync(Guid userId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.RecurringPayments.AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<RecurringPaymentGetDto>
        {
            Items = items.Select(x => MapToDto(x, x.Account.AccountNumber)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<RecurringPaymentGetDto>> GetByIdAsync(Guid userId, Guid id)
    {
        try
        {
            var recurringPayment = await _db.RecurringPayments.AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (recurringPayment == null)
                return new Response<RecurringPaymentGetDto>(HttpStatusCode.NotFound, "Recurring payment not found");

            return new Response<RecurringPaymentGetDto>(HttpStatusCode.OK, "Success", MapToDto(recurringPayment, recurringPayment.Account.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get recurring payment failed");
            return new Response<RecurringPaymentGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> PauseAsync(Guid userId, Guid id)
        => await UpdateStatusAsync(userId, id, RecurringPaymentStatus.Paused, "Recurring payment paused", "RecurringPaymentPaused");

    public async Task<Response<string>> ResumeAsync(Guid userId, Guid id)
        => await UpdateStatusAsync(userId, id, RecurringPaymentStatus.Active, "Recurring payment resumed", "RecurringPaymentResumed");

    public async Task<Response<string>> CancelAsync(Guid userId, Guid id)
        => await UpdateStatusAsync(userId, id, RecurringPaymentStatus.Cancelled, "Recurring payment cancelled", "RecurringPaymentCancelled");

    public async Task<PagedResult<RecurringPaymentHistoryGetDto>> GetHistoryAsync(Guid userId, Guid id, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var recurringPaymentExists = await _db.RecurringPayments.AsNoTracking()
            .AnyAsync(x => x.Id == id && x.UserId == userId);

        if (!recurringPaymentExists)
            return new PagedResult<RecurringPaymentHistoryGetDto>();

        var query = _db.RecurringPaymentHistory.AsNoTracking()
            .Where(x => x.RecurringPaymentId == id);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<RecurringPaymentHistoryGetDto>
        {
            Items = items.Select(MapHistory).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<int>> ExecuteDuePaymentsAsync()
    {
        try
        {
            var duePayments = await _db.RecurringPayments
                .Include(x => x.Account)
                .Where(x => x.Status == RecurringPaymentStatus.Active && x.NextExecutionDate <= DateTime.UtcNow)
                .OrderBy(x => x.NextExecutionDate)
                .ToListAsync();

            var processedCount = 0;
            foreach (var recurringPayment in duePayments)
            {
                await using var dbTransaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    if (!recurringPayment.Account.IsActive)
                    {
                        await MarkExecutionFailureAsync(recurringPayment, "Linked account is inactive");
                        await dbTransaction.CommitAsync();
                        continue;
                    }

                    if (recurringPayment.Account.Balance < recurringPayment.Amount)
                    {
                        await MarkExecutionFailureAsync(recurringPayment, "Insufficient funds");
                        await dbTransaction.CommitAsync();
                        continue;
                    }

                    recurringPayment.Account.Balance -= recurringPayment.Amount;
                    recurringPayment.LastExecutionDate = DateTime.UtcNow;
                    recurringPayment.NextExecutionDate = GetNextExecutionDate(recurringPayment.Frequency, recurringPayment.NextExecutionDate);
                    recurringPayment.AutoRetryCount = 0;
                    recurringPayment.Status = RecurringPaymentStatus.Active;

                    var transaction = new Transaction
                    {
                        FromAccountId = recurringPayment.AccountId,
                        Amount = recurringPayment.Amount,
                        Currency = recurringPayment.CurrencyCode,
                        Type = TransactionType.Withdrawal,
                        Status = TransactionStatus.Completed,
                        Description = $"Recurring payment to {recurringPayment.ProviderName}"
                    };

                    _db.Transactions.Add(transaction);
                    await _db.SaveChangesAsync();

                    _db.RecurringPaymentHistory.Add(new RecurringPaymentHistory
                    {
                        RecurringPaymentId = recurringPayment.Id,
                        TransactionId = transaction.Id,
                        Amount = recurringPayment.Amount,
                        IsSuccess = true,
                        Message = "Recurring payment executed successfully",
                        RetryAttempt = 0
                    });

                    await _db.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    await _notificationService.SendAsync(recurringPayment.UserId, "Recurring payment executed", $"Recurring payment to {recurringPayment.ProviderName} was executed successfully.", "RecurringPayment");
                    await _auditLogService.LogAsync(recurringPayment.UserId, "RecurringPaymentExecuted", "", "", true);
                    processedCount++;
                }
                catch (Exception innerEx)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(innerEx, "Execute recurring payment item failed");
                }
            }

            return new Response<int>(HttpStatusCode.OK, "Recurring payments processed successfully", processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execute due recurring payments failed");
            return new Response<int>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private async Task<Response<string>> UpdateStatusAsync(Guid userId, Guid id, RecurringPaymentStatus targetStatus, string notificationMessage, string auditAction)
    {
        try
        {
            var recurringPayment = await _db.RecurringPayments
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (recurringPayment == null)
                return new Response<string>(HttpStatusCode.NotFound, "Recurring payment not found");

            if (recurringPayment.Status == RecurringPaymentStatus.Cancelled && targetStatus != RecurringPaymentStatus.Cancelled)
                return new Response<string>(HttpStatusCode.BadRequest, "Cancelled recurring payment cannot be reactivated");

            recurringPayment.Status = targetStatus;
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Recurring payment updated", notificationMessage, "RecurringPayment");
            await _auditLogService.LogAsync(userId, auditAction, "", "", true);

            return new Response<string>(HttpStatusCode.OK, notificationMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update recurring payment status failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private async Task MarkExecutionFailureAsync(RecurringPayment recurringPayment, string message)
    {
        recurringPayment.AutoRetryCount++;
        recurringPayment.LastExecutionDate = DateTime.UtcNow;

        if (recurringPayment.AutoRetryCount >= 3)
        {
            recurringPayment.Status = RecurringPaymentStatus.Failed;
        }
        else
        {
            recurringPayment.NextExecutionDate = DateTime.UtcNow.AddDays(1);
        }

        _db.RecurringPaymentHistory.Add(new RecurringPaymentHistory
        {
            RecurringPaymentId = recurringPayment.Id,
            Amount = recurringPayment.Amount,
            IsSuccess = false,
            Message = message,
            RetryAttempt = recurringPayment.AutoRetryCount
        });

        await _db.SaveChangesAsync();
        await _notificationService.SendAsync(recurringPayment.UserId, "Recurring payment failed", $"{message} for {recurringPayment.ProviderName}.", "RecurringPayment");
        await _auditLogService.LogAsync(recurringPayment.UserId, "RecurringPaymentFailed", "", "", false);
    }

    private static DateTime GetNextExecutionDate(RecurringPaymentFrequency frequency, DateTime current)
        => frequency == RecurringPaymentFrequency.Weekly ? current.AddDays(7) : current.AddMonths(1);

    private static RecurringPaymentGetDto MapToDto(RecurringPayment recurringPayment, string accountNumber) => new()
    {
        Id = recurringPayment.Id,
        AccountId = recurringPayment.AccountId,
        AccountNumber = accountNumber,
        ProviderName = recurringPayment.ProviderName,
        Category = recurringPayment.Category.ToString(),
        Amount = recurringPayment.Amount,
        CurrencyCode = recurringPayment.CurrencyCode.ToString(),
        Frequency = recurringPayment.Frequency.ToString(),
        NextExecutionDate = recurringPayment.NextExecutionDate,
        LastExecutionDate = recurringPayment.LastExecutionDate,
        Status = recurringPayment.Status.ToString(),
        AutoRetryCount = recurringPayment.AutoRetryCount,
        Notes = recurringPayment.Notes,
        CreatedAt = recurringPayment.CreatedAt
    };

    private static RecurringPaymentHistoryGetDto MapHistory(RecurringPaymentHistory item) => new()
    {
        Id = item.Id,
        TransactionId = item.TransactionId,
        Amount = item.Amount,
        IsSuccess = item.IsSuccess,
        Message = item.Message,
        RetryAttempt = item.RetryAttempt,
        ExecutedAt = item.ExecutedAt
    };
}
