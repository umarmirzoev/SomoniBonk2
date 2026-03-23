using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IStatsService
{
    Task<Response<object>> GetGeneralStatsAsync();
    Task<Response<object>> GetTransactionStatsAsync();
    Task<Response<object>> GetLoanStatsAsync();
}