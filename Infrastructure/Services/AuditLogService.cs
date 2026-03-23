using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<AuditLogGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var log = await _db.AuditLogs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (log == null)
                return new Response<AuditLogGetDto>(HttpStatusCode.NotFound, "Запись не найдена");

            return new Response<AuditLogGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(log));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAuditLogById failed");
            return new Response<AuditLogGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<AuditLogGetDto>> GetAllAsync(AuditLogFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking();

        if (filter?.UserId != null)
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrEmpty(filter?.Action))
            query = query.Where(x => x.Action == filter.Action);
        if (filter?.IsSuccess != null)
            query = query.Where(x => x.IsSuccess == filter.IsSuccess);
        if (filter?.FromDate != null)
            query = query.Where(x => x.CreatedAt >= filter.FromDate);
        if (filter?.ToDate != null)
            query = query.Where(x => x.CreatedAt <= filter.ToDate);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<AuditLogGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task LogAsync(Guid userId, string action, string ipAddress, string userAgent, bool isSuccess)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogAudit failed");
        }
    }

    private static AuditLogGetDto MapToDto(AuditLog l) => new()
    {
        Id = l.Id,
        UserId = l.UserId,
        Action = l.Action,
        IpAddress = l.IpAddress,
        UserAgent = l.UserAgent,
        IsSuccess = l.IsSuccess,
        CreatedAt = l.CreatedAt
    };
}