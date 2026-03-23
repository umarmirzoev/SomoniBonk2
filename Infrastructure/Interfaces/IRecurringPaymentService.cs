using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IRecurringPaymentService
{
    Task<Response<RecurringPaymentGetDto>> CreateAsync(Guid userId, RecurringPaymentInsertDto dto);
    Task<PagedResult<RecurringPaymentGetDto>> GetMyAsync(Guid userId, PagedQuery pagedQuery);
    Task<Response<RecurringPaymentGetDto>> GetByIdAsync(Guid userId, Guid id);
    Task<Response<string>> PauseAsync(Guid userId, Guid id);
    Task<Response<string>> ResumeAsync(Guid userId, Guid id);
    Task<Response<string>> CancelAsync(Guid userId, Guid id);
    Task<PagedResult<RecurringPaymentHistoryGetDto>> GetHistoryAsync(Guid userId, Guid id, PagedQuery pagedQuery);
    Task<Response<int>> ExecuteDuePaymentsAsync();
}
