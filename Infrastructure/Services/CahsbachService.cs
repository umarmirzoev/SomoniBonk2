using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class CashbackService : ICashbackService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CashbackService> _logger;
    private const decimal CashbackPercent = 1.5m;

    public CashbackService(AppDbContext db, ILogger<CashbackService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<CashbackSummaryDto>> GetSummaryAsync(Guid userId)
    {
        try
        {
            var cashbacks = await _db.Cashbacks.AsNoTracking()
                .Where(x => x.UserId == userId).ToListAsync();

            var summary = new CashbackSummaryDto
            {
                TotalCashback = cashbacks.Sum(x => x.Amount),
                AvailableBalance = cashbacks.Sum(x => x.Amount),
                TotalTransactions = cashbacks.Count
            };

            return new Response<CashbackSummaryDto>(HttpStatusCode.OK, "Успешно", summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCashbackSummary failed");
            return new Response<CashbackSummaryDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<CashbackGetDto>> GetHistoryAsync(Guid userId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.Cashbacks.AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<CashbackGetDto>
        {
            Items = items.Select(x => new CashbackGetDto
            {
                Id = x.Id,
                Amount = x.Amount,
                Percentage = x.Percentage,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task AddCashbackAsync(Guid userId, decimal transactionAmount, string description)
    {
        try
        {
            var cashbackAmount = Math.Round(transactionAmount * CashbackPercent / 100, 2);
            _db.Cashbacks.Add(new Cashback
            {
                UserId = userId,
                Amount = cashbackAmount,
                Percentage = CashbackPercent,
                Description = description
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddCashback failed");
        }
    }
}