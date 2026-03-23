using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<Response<AdminDashboardSummaryDto>> GetSummary()
        => await adminDashboardService.GetSummaryAsync();

    [HttpGet("recent-transactions")]
    public async Task<PagedResult<RecentTransactionAdminDto>> GetRecentTransactions([FromQuery] PagedQuery pagedQuery)
        => await adminDashboardService.GetRecentTransactionsAsync(pagedQuery);

    [HttpGet("recent-logins")]
    public async Task<PagedResult<RecentLoginDto>> GetRecentLogins([FromQuery] PagedQuery pagedQuery)
        => await adminDashboardService.GetRecentLoginsAsync(pagedQuery);

    [HttpGet("pending-kyc")]
    public async Task<Response<List<PendingKycDashboardDto>>> GetPendingKyc()
        => await adminDashboardService.GetPendingKycAsync();

    [HttpGet("open-fraud-alerts")]
    public async Task<Response<List<OpenFraudAlertDashboardDto>>> GetOpenFraudAlerts()
        => await adminDashboardService.GetOpenFraudAlertsAsync();

    [HttpGet("support-tickets")]
    public async Task<PagedResult<SupportTicketDashboardDto>> GetSupportTickets([FromQuery] PagedQuery pagedQuery)
        => await adminDashboardService.GetSupportTicketsAsync(pagedQuery);

    [HttpGet("loan-overview")]
    public async Task<Response<LoanOverviewDto>> GetLoanOverview()
        => await adminDashboardService.GetLoanOverviewAsync();
}
