using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AutoCreditController(IAutoCreditService autoCreditService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<AutoCreditGetDto>> GetAll([FromQuery] AutoCreditFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await autoCreditService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id:guid}")]
    public async Task<Response<AutoCreditGetDto>> GetById(Guid id)
        => await autoCreditService.GetByIdAsync(id);

    [HttpGet("my")]
    public async Task<PagedResult<AutoCreditGetDto>> GetMy([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await autoCreditService.GetMyAsync(userId, pagedQuery);
    }

    [HttpPost("apply")]
    public async Task<Response<AutoCreditGetDto>> Apply(AutoCreditInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await autoCreditService.ApplyAsync(userId, dto);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Approve(Guid id)
        => await autoCreditService.ApproveAsync(id);

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Reject(Guid id)
        => await autoCreditService.RejectAsync(id);

    [HttpPost("pay")]
    public async Task<Response<string>> Pay(AutoCreditPaymentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await autoCreditService.PayAsync(userId, dto);
    }
}
