using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ILoanService
{
    Task<Response<LoanGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<LoanGetDto>> GetAllAsync(LoanFilter filter, PagedQuery pagedQuery);
    Task<Response<string>> ApplyAsync(Guid userId, LoanInsertDto dto);
    Task<Response<string>> ApproveAsync(Guid id);
    Task<Response<string>> RejectAsync(Guid id);
    Task<Response<string>> PayAsync(Guid userId, LoanPaymentDto dto);
}