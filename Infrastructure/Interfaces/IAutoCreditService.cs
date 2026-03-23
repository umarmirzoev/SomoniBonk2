using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IAutoCreditService
{
    Task<Response<AutoCreditGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<AutoCreditGetDto>> GetAllAsync(AutoCreditFilter filter, PagedQuery pagedQuery);
    Task<PagedResult<AutoCreditGetDto>> GetMyAsync(Guid userId, PagedQuery pagedQuery);
    Task<Response<AutoCreditGetDto>> ApplyAsync(Guid userId, AutoCreditInsertDto dto);
    Task<Response<string>> ApproveAsync(Guid id);
    Task<Response<string>> RejectAsync(Guid id);
    Task<Response<string>> PayAsync(Guid userId, AutoCreditPaymentDto dto);
}
