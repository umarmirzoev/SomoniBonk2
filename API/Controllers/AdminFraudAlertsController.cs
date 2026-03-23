using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/admin/fraud-alerts")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminFraudAlertsController(IFraudDetectionService fraudDetectionService) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<FraudAlertGetDto>> GetAll([FromQuery] FraudAlertFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await fraudDetectionService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id:guid}")]
    public async Task<Response<FraudAlertGetDto>> GetById(Guid id)
        => await fraudDetectionService.GetByIdAsync(id);

    [HttpPut("{id:guid}/review")]
    public async Task<Response<string>> Review(Guid id, FraudAlertReviewDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await fraudDetectionService.ReviewAsync(id, adminId, dto);
    }

    [HttpPut("{id:guid}/block")]
    public async Task<Response<string>> Block(Guid id, FraudAlertReviewDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await fraudDetectionService.BlockAsync(id, adminId, dto);
    }

    [HttpPut("{id:guid}/ignore")]
    public async Task<Response<string>> Ignore(Guid id, FraudAlertReviewDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await fraudDetectionService.IgnoreAsync(id, adminId, dto);
    }
}
