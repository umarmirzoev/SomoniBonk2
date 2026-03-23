using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;
using SomoniBank.Domain.DTOs;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditLogController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<AuditLogGetDto>> GetAll([FromQuery] AuditLogFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await auditLogService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<AuditLogGetDto>> GetById(Guid id)
        => await auditLogService.GetByIdAsync(id);
}