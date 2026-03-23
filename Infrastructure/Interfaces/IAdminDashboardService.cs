using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IAdminDashboardService
{
    Task<Response<AdminDashboardSummaryDto>> GetSummaryAsync();
    Task<PagedResult<RecentTransactionAdminDto>> GetRecentTransactionsAsync(PagedQuery pagedQuery);
    Task<PagedResult<RecentLoginDto>> GetRecentLoginsAsync(PagedQuery pagedQuery);
    Task<Response<List<PendingKycDashboardDto>>> GetPendingKycAsync();
    Task<Response<List<OpenFraudAlertDashboardDto>>> GetOpenFraudAlertsAsync();
    Task<PagedResult<SupportTicketDashboardDto>> GetSupportTicketsAsync(PagedQuery pagedQuery);
    Task<Response<LoanOverviewDto>> GetLoanOverviewAsync();
}
