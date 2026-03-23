using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IInternationalTransferService
{
    Task<Response<InternationalTransferGetDto>> CreateAsync(Guid userId, InternationalTransferInsertDto dto);
    Task<Response<InternationalTransferGetDto>> GetByIdAsync(Guid userId, Guid id, bool isAdmin = false);
    Task<PagedResult<InternationalTransferGetDto>> GetMyTransfersAsync(Guid userId, PagedQuery pagedQuery);
    Task<PagedResult<InternationalTransferGetDto>> GetAllAsync(PagedQuery pagedQuery);
}
