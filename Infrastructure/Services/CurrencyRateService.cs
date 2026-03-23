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

public class CurrencyRateService : ICurrencyRateService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CurrencyRateService> _logger;

    public CurrencyRateService(AppDbContext db, ILogger<CurrencyRateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<CurrencyRateGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var rate = await _db.CurrencyRates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (rate == null)
                return new Response<CurrencyRateGetDto>(HttpStatusCode.NotFound, "Курс не найден");

            return new Response<CurrencyRateGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(rate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrencyRateById failed");
            return new Response<CurrencyRateGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<CurrencyRateGetDto>> GetAllAsync(CurrencyRateFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<CurrencyRate> query = _db.CurrencyRates.AsNoTracking();

        if (!string.IsNullOrEmpty(filter?.FromCurrency))
            query = query.Where(x => x.FromCurrency == Enum.Parse<Currency>(filter.FromCurrency, true));
        if (!string.IsNullOrEmpty(filter?.ToCurrency))
            query = query.Where(x => x.ToCurrency == Enum.Parse<Currency>(filter.ToCurrency, true));

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(x => x.FromCurrency)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<CurrencyRateGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> CreateOrUpdateAsync(CurrencyRateInsertDto dto)
    {
        try
        {
            var from = Enum.Parse<Currency>(dto.FromCurrency, true);
            var to = Enum.Parse<Currency>(dto.ToCurrency, true);

            var existing = await _db.CurrencyRates
                .FirstOrDefaultAsync(x => x.FromCurrency == from && x.ToCurrency == to);

            if (existing != null)
            {
                existing.Rate = dto.Rate;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CurrencyRates.Add(new CurrencyRate
                {
                    FromCurrency = from,
                    ToCurrency = to,
                    Rate = dto.Rate
                });
            }

            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Курс обновлён");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrUpdateCurrencyRate failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<decimal>> ConvertAsync(CurrencyConvertDto dto)
    {
        try
        {
            var from = Enum.Parse<Currency>(dto.FromCurrency, true);
            var to = Enum.Parse<Currency>(dto.ToCurrency, true);

            if (from == to)
                return new Response<decimal>(HttpStatusCode.OK, "Успешно", dto.Amount);

            var rate = await _db.CurrencyRates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.FromCurrency == from && x.ToCurrency == to);
            if (rate == null)
                return new Response<decimal>(HttpStatusCode.NotFound, "Курс не найден");

            var result = dto.Amount * rate.Rate;
            return new Response<decimal>(HttpStatusCode.OK, "Успешно", Math.Round(result, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convert failed");
            return new Response<decimal>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    private static CurrencyRateGetDto MapToDto(CurrencyRate r) => new()
    {
        Id = r.Id,
        FromCurrency = r.FromCurrency.ToString(),
        ToCurrency = r.ToCurrency.ToString(),
        Rate = r.Rate,
        UpdatedAt = r.UpdatedAt
    };
}