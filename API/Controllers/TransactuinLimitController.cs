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
public class TransactionLimitController(ITransactionLimitService limitService) : ControllerBase
{
    [HttpGet("{accountId}")]
    public async Task<Response<TransactionLimitGetDto>> GetByAccountId(Guid accountId)
        => await limitService.GetByAccountIdAsync(accountId);

    [HttpPost]
    public async Task<Response<string>> SetLimit(TransactionLimitInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await limitService.SetLimitAsync(userId, dto);
    }
}