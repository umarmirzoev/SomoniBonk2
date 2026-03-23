using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/virtual-cards")]
[ApiController]
[Authorize]
public class VirtualCardController(IVirtualCardService virtualCardService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<VirtualCardCreateResultDto>> Create(VirtualCardInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.CreateAsync(userId, dto);
    }

    [HttpGet("my")]
    public async Task<PagedResult<VirtualCardGetDto>> GetMy([FromQuery] VirtualCardFilter filter, [FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.GetMyAsync(userId, filter, pagedQuery);
    }

    [HttpGet("{id:guid}")]
    public async Task<Response<VirtualCardGetDto>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.GetByIdAsync(userId, id);
    }

    [HttpPut("{id:guid}/freeze")]
    public async Task<Response<string>> Freeze(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.FreezeAsync(userId, id);
    }

    [HttpPut("{id:guid}/unfreeze")]
    public async Task<Response<string>> Unfreeze(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.UnfreezeAsync(userId, id);
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<Response<string>> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.CancelAsync(userId, id);
    }

    [HttpPut("{id:guid}/limits")]
    public async Task<Response<string>> UpdateLimits(Guid id, VirtualCardLimitUpdateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.UpdateLimitsAsync(userId, id, dto);
    }

    [HttpPost("pay")]
    public async Task<Response<VirtualCardPaymentResultDto>> Pay(VirtualCardPaymentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await virtualCardService.UseForPaymentAsync(userId, dto);
    }
}
