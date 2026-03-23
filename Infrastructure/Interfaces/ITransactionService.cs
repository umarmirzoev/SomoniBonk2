using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ITransactionService
{
    Task<Response<TransactionGetDto>> GetByIdAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false);
    Task<PagedResult<TransactionGetDto>> GetAllAsync(TransactionFilter filter, PagedQuery pagedQuery, Guid? requesterUserId = null, bool isAdmin = false);
    Task<Response<string>> TransferAsync(Guid userId, TransferDto dto);
    Task<Response<string>> DepositMoneyAsync(Guid userId, DepositMoneyDto dto);
    Task<Response<string>> WithdrawMoneyAsync(Guid userId, WithdrawMoneyDto dto);
    Task<Response<string>> ExchangeCurrencyAsync(Guid userId, CurrencyExchangeDto dto);
}
