using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ICashbackService
{
    Task<Response<CashbackSummaryDto>> GetSummaryAsync(Guid userId);
    Task<PagedResult<CashbackGetDto>> GetHistoryAsync(Guid userId, PagedQuery pagedQuery);
    Task AddCashbackAsync(Guid userId, decimal transactionAmount, string description);
}