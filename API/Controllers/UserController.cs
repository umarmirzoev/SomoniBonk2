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
public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<UserGetDto>> GetAll([FromQuery] UserFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await userService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<UserGetDto>> GetById(Guid id)
        => await userService.GetByIdAsync(id);

    [HttpGet("me")]
    public async Task<Response<UserGetDto>> GetMe()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await userService.GetByIdAsync(userId);
    }

    [HttpPut("{id}")]
    public async Task<Response<string>> Update(Guid id, UserUpdateDto dto)
        => await userService.UpdateAsync(id, dto);

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Delete(Guid id)
        => await userService.DeleteAsync(id);

    [HttpPatch("{id}/block")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Block(Guid id)
        => await userService.BlockAsync(id);

    [HttpPatch("{id}/unblock")]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> Unblock(Guid id)
        => await userService.UnblockAsync(id);
}