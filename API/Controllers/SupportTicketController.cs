using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/support/tickets")]
[ApiController]
[Authorize]
public class SupportTicketController(ISupportTicketService supportTicketService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<SupportTicketGetDto>> Create(SupportTicketInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.CreateTicketAsync(userId, dto);
    }

    [HttpGet("my")]
    public async Task<PagedResult<SupportTicketGetDto>> GetMy([FromQuery] SupportTicketFilter filter, [FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.GetMyTicketsAsync(userId, filter, pagedQuery);
    }

    [HttpGet("{id:guid}")]
    public async Task<Response<SupportTicketGetDto>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.GetMyTicketByIdAsync(userId, id);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<Response<SupportMessageGetDto>> AddMessage(Guid id, SupportMessageInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.AddUserMessageAsync(userId, id, dto);
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<PagedResult<SupportMessageGetDto>> GetMessages(Guid id, [FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.GetMyMessagesAsync(userId, id, pagedQuery);
    }
}
