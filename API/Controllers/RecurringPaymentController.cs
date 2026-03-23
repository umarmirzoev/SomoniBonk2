using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/recurring-payments")]
[ApiController]
[Authorize]
public class RecurringPaymentController(IRecurringPaymentService recurringPaymentService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<RecurringPaymentGetDto>> Create(RecurringPaymentInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.CreateAsync(userId, dto);
    }

    [HttpGet("my")]
    public async Task<PagedResult<RecurringPaymentGetDto>> GetMy([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.GetMyAsync(userId, pagedQuery);
    }

    [HttpGet("{id:guid}")]
    public async Task<Response<RecurringPaymentGetDto>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.GetByIdAsync(userId, id);
    }

    [HttpPut("{id:guid}/pause")]
    public async Task<Response<string>> Pause(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.PauseAsync(userId, id);
    }

    [HttpPut("{id:guid}/resume")]
    public async Task<Response<string>> Resume(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.ResumeAsync(userId, id);
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<Response<string>> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.CancelAsync(userId, id);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<PagedResult<RecurringPaymentHistoryGetDto>> GetHistory(Guid id, [FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await recurringPaymentService.GetHistoryAsync(userId, id, pagedQuery);
    }

    [HttpPost("process-due")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<int>> ProcessDue()
        => await recurringPaymentService.ExecuteDuePaymentsAsync();
}
