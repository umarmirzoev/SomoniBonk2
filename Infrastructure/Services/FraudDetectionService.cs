using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Filtres;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(
        AppDbContext db,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ILogger<FraudDetectionService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<FraudEvaluationResult> EvaluateTransferAsync(Guid userId, Account fromAccount, decimal amount, string? description)
    {
        try
        {
            var reasons = new List<string>();
            var score = 0;

            var recentTransferCount = await _db.Transactions.AsNoTracking()
                .Where(x => x.FromAccountId == fromAccount.Id
                            && x.Type == TransactionType.Transfer
                            && x.Status == TransactionStatus.Completed
                            && x.CreatedAt >= DateTime.UtcNow.AddMinutes(-15))
                .CountAsync();

            if (recentTransferCount >= 5)
            {
                score += 60;
                reasons.Add("Too many transfers in a short period");
            }
            else if (recentTransferCount >= 3)
            {
                score += 35;
                reasons.Add("Repeated transfer activity detected");
            }

            if (amount >= 10000)
            {
                score += 50;
                reasons.Add("Transfer amount exceeded the high-risk threshold");
            }
            else if (amount >= 5000)
            {
                score += 25;
                reasons.Add("Transfer amount exceeded the review threshold");
            }

            if (await HasRecentFailedLoginPatternAsync(userId))
            {
                score += 25;
                reasons.Add("Transfer requested after multiple failed login attempts");
            }

            if (await HasRecentOpenAlertAsync(userId))
            {
                score += 20;
                reasons.Add("Transfer requested after recent suspicious activity");
            }

            if (!string.IsNullOrWhiteSpace(description) && description.Contains("urgent", StringComparison.OrdinalIgnoreCase))
            {
                score += 5;
                reasons.Add("Transfer description matched a suspicious keyword");
            }

            return await FinalizeEvaluationAsync(userId, null, score, reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evaluate transfer fraud check failed");
            return new FraudEvaluationResult();
        }
    }

    public async Task<FraudEvaluationResult> EvaluateBillPaymentAsync(Guid userId, Account account, decimal amount, string providerName)
    {
        try
        {
            var reasons = new List<string>();
            var score = 0;

            var recentBillCount = await _db.BillPayments.AsNoTracking()
                .Where(x => x.UserId == userId && x.CreatedAt >= DateTime.UtcNow.AddMinutes(-10))
                .CountAsync();

            if (recentBillCount >= 5)
            {
                score += 55;
                reasons.Add("Rapid repeated bill payments detected");
            }
            else if (recentBillCount >= 3)
            {
                score += 30;
                reasons.Add("Elevated bill payment frequency detected");
            }

            if (amount >= 3000)
            {
                score += 20;
                reasons.Add($"Unusually large bill payment to {providerName}");
            }

            if (await HasRecentFailedLoginPatternAsync(userId))
            {
                score += 20;
                reasons.Add("Bill payment requested after failed login attempts");
            }

            if (await HasRecentOpenAlertAsync(userId))
            {
                score += 15;
                reasons.Add("Bill payment requested after recent suspicious activity");
            }

            return await FinalizeEvaluationAsync(userId, null, score, reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evaluate bill payment fraud check failed");
            return new FraudEvaluationResult();
        }
    }

    public async Task<FraudEvaluationResult> EvaluateInternationalTransferAsync(Guid userId, Account fromAccount, decimal amount, string country)
    {
        try
        {
            var reasons = new List<string>();
            var score = 0;

            if (amount >= 8000)
            {
                score += 60;
                reasons.Add("International transfer exceeded the high-risk threshold");
            }
            else if (amount >= 3000)
            {
                score += 35;
                reasons.Add("International transfer amount requires review");
            }

            var recentInternationalCount = await _db.InternationalTransfers.AsNoTracking()
                .Where(x => x.UserId == userId && x.CreatedAt >= DateTime.UtcNow.AddDays(-1))
                .CountAsync();
            if (recentInternationalCount >= 2)
            {
                score += 25;
                reasons.Add("Multiple international transfers in the last 24 hours");
            }

            var distinctCountries = await _db.InternationalTransfers.AsNoTracking()
                .Where(x => x.UserId == userId && x.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .Select(x => x.Country)
                .Distinct()
                .CountAsync();
            if (distinctCountries >= 3)
            {
                score += 20;
                reasons.Add("International transfer pattern spans multiple countries");
            }

            if (await HasRecentFailedLoginPatternAsync(userId))
            {
                score += 25;
                reasons.Add("International transfer requested after failed login attempts");
            }

            if (await HasRecentOpenAlertAsync(userId))
            {
                score += 20;
                reasons.Add("International transfer requested after recent suspicious activity");
            }

            return await FinalizeEvaluationAsync(userId, null, score, reasons.Count == 0 ? [$"International transfer to {country}"] : reasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evaluate international transfer fraud check failed");
            return new FraudEvaluationResult();
        }
    }

    public async Task ProcessFailedLoginAsync(string email, string ipAddress, string userAgent)
    {
        try
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
                return;

            var failedCount = await _db.AuditLogs.AsNoTracking()
                .Where(x => x.UserId == user.Id
                            && x.Action == "Login"
                            && !x.IsSuccess
                            && x.CreatedAt >= DateTime.UtcNow.AddMinutes(-30))
                .CountAsync();

            if (failedCount < 3)
                return;

            var existingAlert = await _db.FraudAlerts.AsNoTracking()
                .AnyAsync(x => x.UserId == user.Id
                               && x.Reason == "Multiple failed login attempts detected"
                               && x.CreatedAt >= DateTime.UtcNow.AddMinutes(-30));
            if (existingAlert)
                return;

            var riskScore = Math.Min(95, 50 + failedCount * 10);
            await CreateAlertAsync(
                user.Id,
                null,
                "Multiple failed login attempts detected",
                riskScore,
                $"IP: {ipAddress}; UserAgent: {userAgent}",
                DetermineRiskLevel(riskScore) >= RiskLevel.High ? FraudStatus.Blocked : FraudStatus.Open);

            await _notificationService.SendAsync(user.Id, "Security alert", "Suspicious login activity was detected on your account.", "Fraud");
            await _auditLogService.LogAsync(user.Id, "FraudAlert:FailedLogin", ipAddress, userAgent, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process failed login fraud detection failed");
        }
    }

    public async Task<Response<string>> ReportBlockedCardUsageAsync(Guid userId, string cardReference, string source, string? notes)
    {
        try
        {
            await CreateAlertAsync(
                userId,
                null,
                $"Blocked or inactive card usage attempt detected in {source}",
                85,
                $"{cardReference}. {notes}",
                FraudStatus.Blocked);

            await _notificationService.SendAsync(userId, "Suspicious card activity", $"A blocked card usage attempt was detected for {cardReference}.", "Fraud");
            await _auditLogService.LogAsync(userId, "FraudAlert:BlockedCardUsage", "system", source, false);

            return new Response<string>(HttpStatusCode.OK, "Fraud alert created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report blocked card usage failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<FraudAlertGetDto>> GetAllAsync(FraudAlertFilter filter, PagedQuery pagedQuery)
    {
        filter ??= new FraudAlertFilter();
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<FraudAlert> query = _db.FraudAlerts.AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<FraudStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.RiskLevel) && Enum.TryParse<RiskLevel>(filter.RiskLevel, true, out var riskLevel))
            query = query.Where(x => x.RiskLevel == riskLevel);
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<FraudAlertGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<FraudAlertGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var alert = await _db.FraudAlerts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (alert == null)
                return new Response<FraudAlertGetDto>(HttpStatusCode.NotFound, "Fraud alert not found");

            return new Response<FraudAlertGetDto>(HttpStatusCode.OK, "Success", MapToDto(alert));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get fraud alert by id failed");
            return new Response<FraudAlertGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public Task<Response<string>> ReviewAsync(Guid id, Guid adminId, FraudAlertReviewDto dto)
        => UpdateStatusAsync(id, adminId, FraudStatus.Reviewed, dto.Notes, "Fraud alert reviewed");

    public Task<Response<string>> BlockAsync(Guid id, Guid adminId, FraudAlertReviewDto dto)
        => UpdateStatusAsync(id, adminId, FraudStatus.Blocked, dto.Notes, "Fraud alert blocked");

    public Task<Response<string>> IgnoreAsync(Guid id, Guid adminId, FraudAlertReviewDto dto)
        => UpdateStatusAsync(id, adminId, FraudStatus.Ignored, dto.Notes, "Fraud alert ignored");

    private async Task<Response<string>> UpdateStatusAsync(Guid id, Guid adminId, FraudStatus status, string? notes, string message)
    {
        try
        {
            var alert = await _db.FraudAlerts.FirstOrDefaultAsync(x => x.Id == id);
            if (alert == null)
                return new Response<string>(HttpStatusCode.NotFound, "Fraud alert not found");

            alert.Status = status;
            alert.Notes = notes;
            alert.ReviewedAt = DateTime.UtcNow;
            alert.ReviewedByAdminId = adminId;

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(alert.UserId, "Fraud case update", $"Your fraud case status changed to {status}.", "Fraud");
            await _auditLogService.LogAsync(adminId, $"FraudAlert:{status}", "system", "admin-panel", true);

            return new Response<string>(HttpStatusCode.OK, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update fraud alert status failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private async Task<FraudEvaluationResult> FinalizeEvaluationAsync(Guid userId, Guid? transactionId, int riskScore, List<string> reasons)
    {
        if (riskScore <= 0 || reasons.Count == 0)
            return new FraudEvaluationResult();

        var riskLevel = DetermineRiskLevel(riskScore);
        var status = riskLevel >= RiskLevel.High ? FraudStatus.Blocked : FraudStatus.Open;
        var reason = string.Join("; ", reasons);
        var alert = await CreateAlertAsync(userId, transactionId, reason, riskScore, null, status);

        await _notificationService.SendAsync(userId, "Suspicious activity detected", $"We detected suspicious activity: {reason}.", "Fraud");
        await _auditLogService.LogAsync(userId, "FraudAlert:AutoCreated", "system", "fraud-engine", riskLevel < RiskLevel.High);

        return new FraudEvaluationResult
        {
            IsBlocked = riskLevel >= RiskLevel.High,
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            Reason = reason,
            FraudAlertId = alert.Id
        };
    }

    private async Task<FraudAlert> CreateAlertAsync(Guid userId, Guid? transactionId, string reason, int riskScore, string? notes, FraudStatus status)
    {
        var alert = new FraudAlert
        {
            UserId = userId,
            TransactionId = transactionId,
            Reason = reason,
            RiskScore = riskScore,
            RiskLevel = DetermineRiskLevel(riskScore),
            Status = status,
            Notes = notes
        };

        _db.FraudAlerts.Add(alert);
        await _db.SaveChangesAsync();
        return alert;
    }

    private async Task<bool> HasRecentFailedLoginPatternAsync(Guid userId)
    {
        var failedCount = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.Action == "Login"
                        && !x.IsSuccess
                        && x.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        return failedCount >= 3;
    }

    private async Task<bool> HasRecentOpenAlertAsync(Guid userId)
    {
        return await _db.FraudAlerts.AsNoTracking()
            .AnyAsync(x => x.UserId == userId
                           && (x.Status == FraudStatus.Open || x.Status == FraudStatus.Blocked)
                           && x.CreatedAt >= DateTime.UtcNow.AddHours(-24));
    }

    private static RiskLevel DetermineRiskLevel(int score) => score switch
    {
        >= 90 => RiskLevel.Critical,
        >= 70 => RiskLevel.High,
        >= 40 => RiskLevel.Medium,
        _ => RiskLevel.Low
    };

    private static FraudAlertGetDto MapToDto(FraudAlert alert) => new()
    {
        Id = alert.Id,
        UserId = alert.UserId,
        TransactionId = alert.TransactionId,
        Reason = alert.Reason,
        RiskScore = alert.RiskScore,
        RiskLevel = alert.RiskLevel.ToString(),
        Status = alert.Status.ToString(),
        CreatedAt = alert.CreatedAt,
        ReviewedAt = alert.ReviewedAt,
        ReviewedByAdminId = alert.ReviewedByAdminId,
        Notes = alert.Notes
    };
}
