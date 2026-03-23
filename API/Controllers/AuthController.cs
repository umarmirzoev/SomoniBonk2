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
    public async Task<ActionResult<Response<string>>> Register([FromBody] UserInsertDto dto)
        => ToHttpResult(await authService.RegisterAsync(dto));

    [HttpPost("login")]
    public async Task<ActionResult<Response<AuthResponseDto>>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        return ToHttpResult(await authService.LoginAsync(request, ipAddress, userAgent));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<Response<string>>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return ToHttpResult(await authService.ChangePasswordAsync(userId, dto));
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
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("create-pin")]
    public async Task<ActionResult<Response<AuthResponseDto>>> CreatePin([FromBody] CreatePinRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        return ToHttpResult(await authService.CreatePinAsync(dto, ipAddress, userAgent));
    }

    [HttpPost("pin-login")]
    public async Task<ActionResult<Response<AuthResponseDto>>> PinLogin([FromBody] PinLoginRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        return ToHttpResult(await authService.LoginWithPinAsync(dto, ipAddress, userAgent));
    }

    private ActionResult<Response<T>> ToHttpResult<T>(Response<T> response)
        => StatusCode(response.StatusCode, response);
}
