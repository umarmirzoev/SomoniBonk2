using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController(IAccountService accountService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<AccountGetDto>> GetAll([FromQuery] AccountFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await accountService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<AccountGetDto>> GetById(Guid id)
        => await accountService.GetByIdAsync(id, CurrentUserId, IsAdmin);

    [HttpGet("{id}/balance")]
    public async Task<Response<decimal>> GetBalance(Guid id)
        => await accountService.GetBalanceAsync(id, CurrentUserId, IsAdmin);

    [HttpGet("my")]
    public async Task<PagedResult<AccountGetDto>> GetMyAccounts([FromQuery] PagedQuery pagedQuery)
    {
        var filter = new AccountFilter { UserId = CurrentUserId };
        return await accountService.GetAllAsync(filter, pagedQuery);
    }

    [HttpPost]
    public async Task<Response<AccountGetDto>> Create(AccountInsertDto dto)
        => await accountService.CreateAsync(CurrentUserId, dto);

    [HttpPatch("{id}/close")]
    public async Task<Response<string>> Close(Guid id)
        => await accountService.CloseAsync(id, CurrentUserId, IsAdmin);
}
