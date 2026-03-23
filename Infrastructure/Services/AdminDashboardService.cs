using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(AppDbContext db, ILogger<AdminDashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<AdminDashboardSummaryDto>> GetSummaryAsync()
    {
        try
        {
            var totalUsers = await _db.Users.AsNoTracking().CountAsync();
            var activeUsers = await _db.Users.AsNoTracking().CountAsync(x => x.IsActive);
            var verifiedUsers = await _db.KycProfiles.AsNoTracking().CountAsync(x => x.Status == KycStatus.Approved);
            var totalAccounts = await _db.Accounts.AsNoTracking().CountAsync();
            var totalCards = await _db.Cards.AsNoTracking().CountAsync();
            var totalVirtualCards = await _db.VirtualCards.AsNoTracking().CountAsync();
            var totalDeposits = await _db.Deposits.AsNoTracking().CountAsync();
            var totalLoans = await _db.Loans.AsNoTracking().CountAsync() + await _db.AutoCredits.AsNoTracking().CountAsync();
            var totalTransferVolume = await _db.Transactions.AsNoTracking()
                .Where(x => x.Type == TransactionType.Transfer && x.Status == TransactionStatus.Completed)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var pendingKyc = await _db.KycProfiles.AsNoTracking().CountAsync(x => x.Status == KycStatus.Pending || x.Status == KycStatus.UnderReview);
            var openFraudAlerts = await _db.FraudAlerts.AsNoTracking().CountAsync(x => x.Status == FraudStatus.Open || x.Status == FraudStatus.Blocked);
            var openSupportTickets = await _db.SupportTickets.AsNoTracking().CountAsync(x => x.Status == SupportTicketStatus.Open || x.Status == SupportTicketStatus.InProgress);

            return new Response<AdminDashboardSummaryDto>(HttpStatusCode.OK, "Success", new AdminDashboardSummaryDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                VerifiedUsers = verifiedUsers,
                TotalAccounts = totalAccounts,
                TotalCards = totalCards,
                TotalVirtualCards = totalVirtualCards,
                TotalDeposits = totalDeposits,
                TotalLoans = totalLoans,
                TotalTransfersVolume = totalTransferVolume,
                PendingKycRequests = pendingKyc,
                OpenFraudAlerts = openFraudAlerts,
                OpenSupportTickets = openSupportTickets,
                SystemHealthSummary = openFraudAlerts > 10 || pendingKyc > 20 || openSupportTickets > 25 ? "Attention required" : "Healthy",
                TopActiveUsers = await GetTopActiveUsersAsync()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get admin dashboard summary failed");
            return new Response<AdminDashboardSummaryDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<RecentTransactionAdminDto>> GetRecentTransactionsAsync(PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.Transactions.AsNoTracking()
            .Include(x => x.FromAccount)
            .Include(x => x.ToAccount)
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<RecentTransactionAdminDto>
        {
            Items = items.Select(x => new RecentTransactionAdminDto
            {
                Id = x.Id,
                FromAccountNumber = x.FromAccount?.AccountNumber,
                ToAccountNumber = x.ToAccount?.AccountNumber,
                Amount = x.Amount,
                Currency = x.Currency.ToString(),
                Type = x.Type.ToString(),
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResult<RecentLoginDto>> GetRecentLoginsAsync(PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.AuditLogs.AsNoTracking()
            .Where(x => x.Action == "Login")
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<RecentLoginDto>
        {
            Items = items.Select(x => new RecentLoginDto
            {
                AuditLogId = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                IsSuccess = x.IsSuccess,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<List<PendingKycDashboardDto>>> GetPendingKycAsync()
    {
        try
        {
            var items = await _db.KycProfiles.AsNoTracking()
                .Where(x => x.Status == KycStatus.Pending || x.Status == KycStatus.UnderReview)
                .OrderBy(x => x.SubmittedAt)
                .Take(20)
                .Select(x => new PendingKycDashboardDto
                {
                    KycProfileId = x.Id,
                    UserId = x.UserId,
                    FullName = x.FullName,
                    SubmittedAt = x.SubmittedAt,
                    Status = x.Status.ToString()
                })
                .ToListAsync();

            return new Response<List<PendingKycDashboardDto>>(HttpStatusCode.OK, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get pending kyc dashboard data failed");
            return new Response<List<PendingKycDashboardDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<List<OpenFraudAlertDashboardDto>>> GetOpenFraudAlertsAsync()
    {
        try
        {
            var items = await _db.FraudAlerts.AsNoTracking()
                .Where(x => x.Status == FraudStatus.Open || x.Status == FraudStatus.Blocked)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .Select(x => new OpenFraudAlertDashboardDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Reason = x.Reason,
                    RiskScore = x.RiskScore,
                    RiskLevel = x.RiskLevel.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new Response<List<OpenFraudAlertDashboardDto>>(HttpStatusCode.OK, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get open fraud alerts dashboard data failed");
            return new Response<List<OpenFraudAlertDashboardDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<SupportTicketDashboardDto>> GetSupportTicketsAsync(PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.SupportTickets.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<SupportTicketDashboardDto>
        {
            Items = items.Select(x => new SupportTicketDashboardDto
            {
                TicketId = x.Id,
                UserId = x.UserId,
                Subject = x.Subject,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<LoanOverviewDto>> GetLoanOverviewAsync()
    {
        try
        {
            var overview = new LoanOverviewDto
            {
                PendingLoans = await _db.Loans.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Pending),
                ActiveLoans = await _db.Loans.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Active),
                PaidLoans = await _db.Loans.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Paid),
                RejectedLoans = await _db.Loans.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Rejected),
                PendingAutoCredits = await _db.AutoCredits.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Pending),
                ActiveAutoCredits = await _db.AutoCredits.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Active),
                PaidAutoCredits = await _db.AutoCredits.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Paid),
                RejectedAutoCredits = await _db.AutoCredits.AsNoTracking().CountAsync(x => x.Status == LoanStatus.Rejected)
            };

            return new Response<LoanOverviewDto>(HttpStatusCode.OK, "Success", overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get loan overview failed");
            return new Response<LoanOverviewDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private async Task<List<TopActiveUserDto>> GetTopActiveUsersAsync()
    {
        var outgoing = await _db.Transactions.AsNoTracking()
            .Where(x => x.Status == TransactionStatus.Completed && x.FromAccountId != null)
            .Join(_db.Accounts.AsNoTracking(), x => x.FromAccountId!.Value, a => a.Id, (x, a) => new { a.UserId, x.Amount })
            .ToListAsync();

        var users = await _db.Users.AsNoTracking()
            .Select(x => new { x.Id, FullName = x.FirstName + " " + x.LastName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        return outgoing
            .GroupBy(x => x.UserId)
            .Select(x => new TopActiveUserDto
            {
                UserId = x.Key,
                FullName = users.TryGetValue(x.Key, out var fullName) ? fullName : "Unknown",
                TransactionCount = x.Count(),
                TransactionVolume = x.Sum(v => v.Amount)
            })
            .OrderByDescending(x => x.TransactionCount)
            .ThenByDescending(x => x.TransactionVolume)
            .Take(5)
            .ToList();
    }
}
