using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IInstallmentService
{
    Task<Response<InstallmentGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<InstallmentGetDto>> GetAllAsync(InstallmentFilter filter, PagedQuery pagedQuery);
    Task<Response<string>> CreateAsync(Guid userId, InstallmentInsertDto dto);
    Task<Response<string>> PayAsync(Guid userId, InstallmentPaymentDto dto);
    Task<Response<string>> CancelAsync(Guid id);
}