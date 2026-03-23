using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    [HttpGet("my")]
    public async Task<PagedResult<NotificationGetDto>> GetMy([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await notificationService.GetMyNotificationsAsync(userId, pagedQuery);
    }

    [HttpPatch("{id}/read")]
    public async Task<Response<string>> MarkAsRead(Guid id)
        => await notificationService.MarkAsReadAsync(id);

    [HttpPatch("read-all")]
    public async Task<Response<string>> MarkAllAsRead()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await notificationService.MarkAllAsReadAsync(userId);
    }
}