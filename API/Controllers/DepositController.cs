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
public class DepositController(IDepositService depositService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<DepositGetDto>> GetAll([FromQuery] DepositFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await depositService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<DepositGetDto>> GetById(Guid id)
        => await depositService.GetByIdAsync(id);

    [HttpGet("my")]
    public async Task<PagedResult<DepositGetDto>> GetMyDeposits([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var filter = new DepositFilter();
        return await depositService.GetAllAsync(filter, pagedQuery);
    }

    [HttpPost]
    public async Task<Response<string>> Create(DepositInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await depositService.CreateAsync(userId, dto);
    }

    [HttpPatch("{id}/close")]
    public async Task<Response<string>> Close(Guid id)
        => await depositService.CloseAsync(id);
}