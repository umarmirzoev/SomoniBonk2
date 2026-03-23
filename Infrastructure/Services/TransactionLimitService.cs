using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class TransactionLimitService : ITransactionLimitService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TransactionLimitService> _logger;

    public TransactionLimitService(AppDbContext db, ILogger<TransactionLimitService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<TransactionLimitGetDto>> GetByAccountIdAsync(Guid accountId)
    {
        try
        {
            var limit = await _db.TransactionLimits.AsNoTracking()
                .FirstOrDefaultAsync(x => x.AccountId == accountId);
            if (limit == null)
                return new Response<TransactionLimitGetDto>(HttpStatusCode.NotFound, "Лимит не найден");

            return new Response<TransactionLimitGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(limit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTransactionLimit failed");
            return new Response<TransactionLimitGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> SetLimitAsync(Guid userId, TransactionLimitInsertDto dto)
    {
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Счёт не найден");

            var existing = await _db.TransactionLimits
                .FirstOrDefaultAsync(x => x.AccountId == dto.AccountId);

            if (existing != null)
            {
                existing.DailyLimit = dto.DailyLimit;
                existing.SingleTransactionLimit = dto.SingleTransactionLimit;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.TransactionLimits.Add(new TransactionLimit
                {
                    AccountId = dto.AccountId,
                    DailyLimit = dto.DailyLimit,
                    SingleTransactionLimit = dto.SingleTransactionLimit
                });
            }

            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Лимит установлен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetTransactionLimit failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<bool> CheckLimitAsync(Guid accountId, decimal amount)
    {
        var limit = await _db.TransactionLimits
            .FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (limit == null) return true;

        if (limit.LastResetDate.Date < DateTime.UtcNow.Date)
        {
            limit.UsedTodayAmount = 0;
            limit.LastResetDate = DateTime.UtcNow.Date;
            await _db.SaveChangesAsync();
        }

        if (amount > limit.SingleTransactionLimit) return false;
        if (limit.UsedTodayAmount + amount > limit.DailyLimit) return false;

        return true;
    }

    public async Task UpdateUsedAmountAsync(Guid accountId, decimal amount)
    {
        var limit = await _db.TransactionLimits
            .FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (limit == null) return;

        limit.UsedTodayAmount += amount;
        await _db.SaveChangesAsync();
    }

    private static TransactionLimitGetDto MapToDto(TransactionLimit l) => new()
    {
        Id = l.Id,
        AccountId = l.AccountId,
        DailyLimit = l.DailyLimit,
        SingleTransactionLimit = l.SingleTransactionLimit,
        UsedTodayAmount = l.UsedTodayAmount,
        UpdatedAt = l.UpdatedAt
    };
}