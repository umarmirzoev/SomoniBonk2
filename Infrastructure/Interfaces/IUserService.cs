using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IUserService
{
    Task<Response<UserGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<UserGetDto>> GetAllAsync(UserFilter filter, PagedQuery pagedQuery);
    Task<Response<string>> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<Response<string>> DeleteAsync(Guid id);
    Task<Response<string>> BlockAsync(Guid id);
    Task<Response<string>> UnblockAsync(Guid id);
}