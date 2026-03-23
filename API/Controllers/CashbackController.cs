using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CashbackController(ICashbackService cashbackService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<Response<CashbackSummaryDto>> GetSummary()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await cashbackService.GetSummaryAsync(userId);
    }

    [HttpGet("history")]
    public async Task<PagedResult<CashbackGetDto>> GetHistory([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await cashbackService.GetHistoryAsync(userId, pagedQuery);
    }
}