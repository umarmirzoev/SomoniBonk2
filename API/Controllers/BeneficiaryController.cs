using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/beneficiaries")]
[ApiController]
[Authorize]
public class BeneficiaryController(IBeneficiaryService beneficiaryService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<BeneficiaryGetDto>> Create(BeneficiaryInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.CreateAsync(userId, dto);
    }

    [HttpGet]
    public async Task<PagedResult<BeneficiaryGetDto>> GetAll([FromQuery] BeneficiaryFilter filter, [FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.GetAllAsync(userId, filter, pagedQuery);
    }

    [HttpGet("{id:guid}")]
    public async Task<Response<BeneficiaryGetDto>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.GetByIdAsync(userId, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<Response<BeneficiaryGetDto>> Update(Guid id, BeneficiaryUpdateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.UpdateAsync(userId, id, dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<Response<string>> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.DeleteAsync(userId, id);
    }

    [HttpPut("{id:guid}/favorite")]
    public async Task<Response<string>> Favorite(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.SetFavoriteAsync(userId, id, true);
    }

    [HttpPut("{id:guid}/unfavorite")]
    public async Task<Response<string>> Unfavorite(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await beneficiaryService.SetFavoriteAsync(userId, id, false);
    }
}
