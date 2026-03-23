using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/admin/kyc")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminKycController(IKycService kycService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<PagedResult<KycProfileGetDto>> GetPending([FromQuery] PagedQuery pagedQuery)
        => await kycService.GetPendingAsync(pagedQuery);

    [HttpGet("all")]
    public async Task<PagedResult<KycProfileGetDto>> GetAll([FromQuery] KycFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await kycService.GetAllAsync(filter, pagedQuery);

    [HttpPut("{id:guid}/approve")]
    public async Task<Response<string>> Approve(Guid id)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await kycService.ApproveAsync(adminId, id);
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<Response<string>> Reject(Guid id, KycReviewDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await kycService.RejectAsync(adminId, id, dto);
    }
}
