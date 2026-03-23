using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, ISmsVerificationService smsVerificationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<Response<string>> Register(UserInsertDto dto)
        => await authService.RegisterAsync(dto);

    [HttpPost("login")]
    public async Task<Response<AuthResponseDto>> Login(LoginDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        return await authService.LoginAsync(dto, ipAddress, userAgent);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<Response<string>> ChangePassword(ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await authService.ChangePasswordAsync(userId, dto);
    }

    [HttpPost("send-code")]
    public async Task<IActionResult> SendCode([FromBody] SendCodeRequestDto dto, CancellationToken cancellationToken)
    {
        var response = await smsVerificationService.SendCodeAsync(dto.Phone, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("verify-code")]
    public async Task<ActionResult<VerifyResult>> VerifyCode([FromBody] VerifyCodeRequestDto dto, CancellationToken cancellationToken)
    {
        var response = await smsVerificationService.VerifyCodeAsync(dto.Phone, dto.Code, cancellationToken);
        return Ok(response);
    }

    [HttpPost("create-pin")]
    public async Task<Response<AuthResponseDto>> CreatePin([FromBody] CreatePinRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        return await authService.CreatePinAsync(dto, ipAddress, userAgent);
    }

    [HttpPost("pin-login")]
    public async Task<Response<AuthResponseDto>> PinLogin([FromBody] PinLoginRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        return await authService.LoginWithPinAsync(dto, ipAddress, userAgent);
    }
}
