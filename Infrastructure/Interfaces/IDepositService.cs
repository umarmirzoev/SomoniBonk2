using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IDepositService
{
    Task<Response<DepositGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<DepositGetDto>> GetAllAsync(DepositFilter filter, PagedQuery pagedQuery);
    Task<Response<string>> CreateAsync(Guid userId, DepositInsertDto dto);
    Task<Response<string>> CloseAsync(Guid id);
}