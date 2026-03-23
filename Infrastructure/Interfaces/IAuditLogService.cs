using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IAuditLogService
{
    Task<Response<AuditLogGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<AuditLogGetDto>> GetAllAsync(AuditLogFilter filter, PagedQuery pagedQuery);
    Task LogAsync(Guid userId, string action, string ipAddress, string userAgent, bool isSuccess);
}