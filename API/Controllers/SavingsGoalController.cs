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
public class SavingsGoalController(ISavingsGoalService savingsGoalService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<SavingsGoalGetDto>> Create(SavingsGoalInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await savingsGoalService.CreateAsync(userId, dto);
    }

    [HttpGet("my")]
    public async Task<PagedResult<SavingsGoalGetDto>> GetMyGoals([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await savingsGoalService.GetMyGoalsAsync(userId, pagedQuery);
    }

    [HttpPost("{goalId:guid}/deposit")]
    public async Task<Response<SavingsGoalGetDto>> DepositToGoal(Guid goalId, SavingsGoalDepositDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await savingsGoalService.DepositToGoalAsync(userId, goalId, dto);
    }

    [HttpPost("{goalId:guid}/complete")]
    public async Task<Response<SavingsGoalGetDto>> CompleteGoal(Guid goalId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await savingsGoalService.CompleteGoalAsync(userId, goalId);
    }
}
