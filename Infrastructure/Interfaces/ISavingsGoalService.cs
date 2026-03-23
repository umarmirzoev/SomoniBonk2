using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ISavingsGoalService
{
    Task<Response<SavingsGoalGetDto>> CreateAsync(Guid userId, SavingsGoalInsertDto dto);
    Task<PagedResult<SavingsGoalGetDto>> GetMyGoalsAsync(Guid userId, PagedQuery pagedQuery);
    Task<Response<SavingsGoalGetDto>> DepositToGoalAsync(Guid userId, Guid goalId, SavingsGoalDepositDto dto);
    Task<Response<SavingsGoalGetDto>> CompleteGoalAsync(Guid userId, Guid goalId);
}
