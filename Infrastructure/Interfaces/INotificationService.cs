using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationGetDto>> GetMyNotificationsAsync(Guid userId, PagedQuery pagedQuery);
    Task<Response<string>> MarkAsReadAsync(Guid id);
    Task<Response<string>> MarkAllAsReadAsync(Guid userId);
    Task SendAsync(Guid userId, string title, string message, string type);
}