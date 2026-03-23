using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IVirtualCardService
{
    Task<Response<VirtualCardCreateResultDto>> CreateAsync(Guid userId, VirtualCardInsertDto dto);
    Task<PagedResult<VirtualCardGetDto>> GetMyAsync(Guid userId, VirtualCardFilter filter, PagedQuery pagedQuery);
    Task<Response<VirtualCardGetDto>> GetByIdAsync(Guid userId, Guid id);
    Task<Response<string>> FreezeAsync(Guid userId, Guid id);
    Task<Response<string>> UnfreezeAsync(Guid userId, Guid id);
    Task<Response<string>> CancelAsync(Guid userId, Guid id);
    Task<Response<string>> UpdateLimitsAsync(Guid userId, Guid id, VirtualCardLimitUpdateDto dto);
    Task<Response<VirtualCardPaymentResultDto>> UseForPaymentAsync(Guid userId, VirtualCardPaymentDto dto);
}
