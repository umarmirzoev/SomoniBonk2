using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SomoniBank.Application.AI.Interfaces;

namespace SomoniBank.Infrastructure.AI.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => GetClaimValue(ClaimTypes.NameIdentifier);

    public string? PhoneNumber
        => GetClaimValue(ClaimTypes.MobilePhone)
           ?? GetClaimValue("phone")
           ?? GetClaimValue("Phone");

    public string? Role => GetClaimValue(ClaimTypes.Role);

    private string? GetClaimValue(string claimType)
        => httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
}
