using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ITransactionLimitService
{
    Task<Response<TransactionLimitGetDto>> GetByAccountIdAsync(Guid accountId);
    Task<Response<string>> SetLimitAsync(Guid userId, TransactionLimitInsertDto dto);
    Task<bool> CheckLimitAsync(Guid accountId, decimal amount);
    Task UpdateUsedAmountAsync(Guid accountId, decimal amount);
}
