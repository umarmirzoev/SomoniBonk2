using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.API.Controllers;

[Route("api/kyc")]
[ApiController]
[Authorize]
public class KycController(IKycService kycService) : ControllerBase
{
    [HttpPost("submit")]
    public async Task<Infrastructure.Responses.Response<KycProfileGetDto>> Submit(KycSubmitDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await kycService.SubmitAsync(userId, dto);
    }

    [HttpGet("my-status")]
    public async Task<Infrastructure.Responses.Response<KycStatusGetDto>> GetMyStatus()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await kycService.GetMyStatusAsync(userId);
    }

    [HttpGet("my-profile")]
    public async Task<Infrastructure.Responses.Response<KycProfileGetDto>> GetMyProfile()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await kycService.GetMyProfileAsync(userId);
    }

    [HttpPut("update")]
    public async Task<Infrastructure.Responses.Response<KycProfileGetDto>> Update(KycUpdateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await kycService.UpdateAsync(userId, dto);
    }
}
