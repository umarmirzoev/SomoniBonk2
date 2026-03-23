using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class QrPaymentController(IQrPaymentService qrPaymentService) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<Infrastructure.Responses.Response<QrPaymentGetDto>> Generate(GenerateQrDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await qrPaymentService.GenerateQrAsync(userId, dto);
    }

    [HttpPost("pay")]
    public async Task<Infrastructure.Responses.Response<QrPaymentGetDto>> Pay(PayByQrDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await qrPaymentService.PayByQrAsync(userId, dto);
    }

    [HttpGet("status/{qrCode}")]
    public async Task<Infrastructure.Responses.Response<QrPaymentGetDto>> GetStatus(string qrCode)
        => await qrPaymentService.GetQrStatusAsync(qrCode);
}
