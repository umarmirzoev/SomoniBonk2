using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IAnalyticsService
{
    Task<Response<IncomeVsExpenseDto>> GetIncomeVsExpenseAsync(Guid userId, AnalyticsPeriodDto period);
    Task<Response<List<SpendingByCategoryDto>>> GetSpendingByCategoryAsync(Guid userId, AnalyticsPeriodDto period);
    Task<Response<List<MonthlyStatsDto>>> GetMonthlyStatsAsync(Guid userId, int year);
    Task<Response<List<TopRecipientDto>>> GetTopRecipientsAsync(Guid userId, AnalyticsPeriodDto period);
    Task<Response<object>> GetDailySpendingAsync(Guid userId, AnalyticsPeriodDto period);
}
