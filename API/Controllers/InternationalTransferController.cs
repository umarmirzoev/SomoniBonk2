using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InternationalTransferController(IInternationalTransferService internationalTransferService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<InternationalTransferGetDto>> Create(InternationalTransferInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await internationalTransferService.CreateAsync(userId, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<Response<InternationalTransferGetDto>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");
        return await internationalTransferService.GetByIdAsync(userId, id, isAdmin);
    }

    [HttpGet("my")]
    public async Task<PagedResult<InternationalTransferGetDto>> GetMyTransfers([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await internationalTransferService.GetMyTransfersAsync(userId, pagedQuery);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<InternationalTransferGetDto>> GetAll([FromQuery] PagedQuery pagedQuery)
        => await internationalTransferService.GetAllAsync(pagedQuery);
}
