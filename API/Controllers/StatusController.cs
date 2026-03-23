using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class StatsController(IStatsService statsService) : ControllerBase
{
    [HttpGet("general")]
    public async Task<Response<object>> GetGeneral()
        => await statsService.GetGeneralStatsAsync();

    [HttpGet("transactions")]
    public async Task<Response<object>> GetTransactions()
        => await statsService.GetTransactionStatsAsync();

    [HttpGet("loans")]
    public async Task<Response<object>> GetLoans()
        => await statsService.GetLoanStatsAsync();
}