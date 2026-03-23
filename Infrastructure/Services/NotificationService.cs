using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<NotificationGetDto>> GetMyNotificationsAsync(Guid userId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<NotificationGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> MarkAsReadAsync(Guid id)
    {
        try
        {
            var notification = await _db.Notifications.FindAsync(id);
            if (notification == null)
                return new Response<string>(HttpStatusCode.NotFound, "Уведомление не найдено");

            notification.IsRead = true;
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Отмечено как прочитанное");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkAsRead failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var notifications = await _db.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync();

            foreach (var n in notifications)
                n.IsRead = true;

            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, $"Прочитано {notifications.Count} уведомлений");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkAllAsRead failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task SendAsync(Guid userId, string title, string message, string type)
    {
        try
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendNotification failed");
        }
    }

    private static NotificationGetDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        Type = n.Type,
        CreatedAt = n.CreatedAt
    };
}