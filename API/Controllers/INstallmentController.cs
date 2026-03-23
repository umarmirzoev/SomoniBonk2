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
public class InstallmentController(IInstallmentService installmentService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<InstallmentGetDto>> GetAll([FromQuery] InstallmentFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await installmentService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<InstallmentGetDto>> GetById(Guid id)
        => await installmentService.GetByIdAsync(id);

    [HttpGet("my")]
    public async Task<PagedResult<InstallmentGetDto>> GetMy([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var filter = new InstallmentFilter { UserId = userId };
        return await installmentService.GetAllAsync(filter, pagedQuery);
    }

    [HttpPost]
    public async Task<Response<string>> Create(InstallmentInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await installmentService.CreateAsync(userId, dto);
    }

    [HttpPost("pay")]
    public async Task<Response<string>> Pay(InstallmentPaymentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await installmentService.PayAsync(userId, dto);
    }

    [HttpPatch("{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Cancel(Guid id)
        => await installmentService.CancelAsync(id);
}