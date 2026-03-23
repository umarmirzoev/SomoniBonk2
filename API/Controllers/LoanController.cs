using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LoanController(ILoanService loanService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<LoanGetDto>> GetAll([FromQuery] LoanFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await loanService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<LoanGetDto>> GetById(Guid id)
        => await loanService.GetByIdAsync(id);

    [HttpGet("my")]
    public async Task<PagedResult<LoanGetDto>> GetMyLoans([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var filter = new LoanFilter { UserId = userId };
        return await loanService.GetAllAsync(filter, pagedQuery);
    }

    [HttpPost("apply")]
    public async Task<Response<string>> Apply(LoanInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await loanService.ApplyAsync(userId, dto);
    }

    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Approve(Guid id)
        => await loanService.ApproveAsync(id);

    [HttpPatch("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Reject(Guid id)
        => await loanService.RejectAsync(id);

    [HttpPost("pay")]
    public async Task<Response<string>> Pay(LoanPaymentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await loanService.PayAsync(userId, dto);
    }
}