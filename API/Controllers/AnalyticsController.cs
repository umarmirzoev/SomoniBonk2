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
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("income-vs-expense")]
    public async Task<Response<IncomeVsExpenseDto>> GetIncomeVsExpense([FromQuery] AnalyticsPeriodDto period)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await analyticsService.GetIncomeVsExpenseAsync(userId, period);
    }

    [HttpGet("spending-by-category")]
    public async Task<Response<List<SpendingByCategoryDto>>> GetSpendingByCategory([FromQuery] AnalyticsPeriodDto period)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await analyticsService.GetSpendingByCategoryAsync(userId, period);
    }

    [HttpGet("monthly/{year:int}")]
    public async Task<Response<List<MonthlyStatsDto>>> GetMonthlyStats(int year)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await analyticsService.GetMonthlyStatsAsync(userId, year);
    }

    [HttpGet("top-recipients")]
    public async Task<Response<List<TopRecipientDto>>> GetTopRecipients([FromQuery] AnalyticsPeriodDto period)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await analyticsService.GetTopRecipientsAsync(userId, period);
    }

    [HttpGet("daily-spending")]
    public async Task<Response<object>> GetDailySpending([FromQuery] AnalyticsPeriodDto period)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await analyticsService.GetDailySpendingAsync(userId, period);
    }
}
