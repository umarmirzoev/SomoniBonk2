using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IAccountService
{
    Task<Response<AccountGetDto>> GetByIdAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false);
    Task<PagedResult<AccountGetDto>> GetAllAsync(AccountFilter filter, PagedQuery pagedQuery);
    Task<Response<AccountGetDto>> CreateAsync(Guid userId, AccountInsertDto dto);
    Task<Response<string>> CloseAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false);
    Task<Response<decimal>> GetBalanceAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false);
}
