using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/admin/support/tickets")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminSupportTicketController(ISupportTicketService supportTicketService) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<SupportTicketGetDto>> GetAll([FromQuery] SupportTicketFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await supportTicketService.GetAllTicketsAsync(filter, pagedQuery);

    [HttpGet("{id:guid}")]
    public async Task<Response<SupportTicketGetDto>> GetById(Guid id)
        => await supportTicketService.GetTicketByIdAsync(id);

    [HttpGet("{id:guid}/messages")]
    public async Task<PagedResult<SupportMessageGetDto>> GetMessages(Guid id, [FromQuery] PagedQuery pagedQuery)
        => await supportTicketService.GetAdminMessagesAsync(id, pagedQuery);

    [HttpPut("{id:guid}/assign")]
    public async Task<Response<string>> Assign(Guid id)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.AssignTicketAsync(adminId, id);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<Response<string>> UpdateStatus(Guid id, SupportTicketStatusUpdateDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.UpdateStatusAsync(adminId, id, dto);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<Response<SupportMessageGetDto>> AddMessage(Guid id, SupportMessageInsertDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await supportTicketService.AddAdminMessageAsync(adminId, id, dto);
    }
}
