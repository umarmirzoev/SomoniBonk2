using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.API.Controllers;

[Route("api/credit-scoring")]
[ApiController]
[Authorize]
public class CreditScoringController(ICreditScoringService creditScoringService) : ControllerBase
{
    [HttpPost("calculate")]
    public async Task<Infrastructure.Responses.Response<CreditScoreResultDto>> Calculate(CreditScoreCalculateDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await creditScoringService.CalculateAsync(userId, dto);
    }

    [HttpGet("my-latest")]
    public async Task<Infrastructure.Responses.Response<CreditScoreResultDto>> GetMyLatest()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await creditScoringService.GetLatestAsync(userId);
    }

    [HttpGet("/api/admin/credit-scoring/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<Infrastructure.Responses.Response<CreditScoreResultDto>> GetForAdmin(Guid userId)
        => await creditScoringService.GetLatestForAdminAsync(userId);

    [HttpGet("/api/admin/credit-scoring/history/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<Infrastructure.Responses.Response<List<CreditScoreResultDto>>> GetHistory(Guid userId)
        => await creditScoringService.GetHistoryAsync(userId);
}
