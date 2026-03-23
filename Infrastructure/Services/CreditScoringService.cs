using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class CreditScoringService : ICreditScoringService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CreditScoringService> _logger;

    public CreditScoringService(AppDbContext db, ILogger<CreditScoringService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<CreditScoreResultDto>> CalculateAsync(Guid userId, CreditScoreCalculateDto dto)
    {
        try
        {
            var userExists = await _db.Users.AsNoTracking().AnyAsync(x => x.Id == userId);
            if (!userExists)
                return new Response<CreditScoreResultDto>(HttpStatusCode.NotFound, "User not found");

            if (dto.MonthlyIncome < 0 || dto.ExistingDebt < 0 || dto.CreditHistoryLengthMonths < 0 || dto.MissedPaymentsCount < 0)
                return new Response<CreditScoreResultDto>(HttpStatusCode.BadRequest, "Invalid credit scoring data");

            var result = await CalculateAndPersistAsync(userId, dto);
            return new Response<CreditScoreResultDto>(HttpStatusCode.OK, "Credit score calculated", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calculate credit score failed");
            return new Response<CreditScoreResultDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<CreditScoreResultDto>> GetLatestAsync(Guid userId)
    {
        try
        {
            var profile = await _db.CreditScoreProfiles.AsNoTracking()
                .OrderByDescending(x => x.CalculatedAt)
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
                return new Response<CreditScoreResultDto>(HttpStatusCode.NotFound, "Credit score not found");

            return new Response<CreditScoreResultDto>(HttpStatusCode.OK, "Success", MapToDto(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get latest credit score failed");
            return new Response<CreditScoreResultDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public Task<Response<CreditScoreResultDto>> GetLatestForAdminAsync(Guid userId) => GetLatestAsync(userId);

    public async Task<Response<List<CreditScoreResultDto>>> GetHistoryAsync(Guid userId)
    {
        try
        {
            var items = await _db.CreditScoreHistory.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CalculatedAt)
                .Select(x => MapToDto(x))
                .ToListAsync();

            return new Response<List<CreditScoreResultDto>>(HttpStatusCode.OK, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get credit score history failed");
            return new Response<List<CreditScoreResultDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<CreditScoreDecisionContext> EvaluateApplicationAsync(Guid userId, decimal requestedAmount, string note)
    {
        try
        {
            var latestProfile = await _db.CreditScoreProfiles.AsNoTracking()
                .OrderByDescending(x => x.CalculatedAt)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            var dto = latestProfile == null
                ? await BuildDerivedInputAsync(userId, requestedAmount, note)
                : new CreditScoreCalculateDto
                {
                    MonthlyIncome = latestProfile.MonthlyIncome,
                    EmploymentStatus = latestProfile.EmploymentStatus,
                    ExistingDebt = await GetExistingDebtAsync(userId),
                    CreditHistoryLengthMonths = await GetCreditHistoryLengthMonthsAsync(userId),
                    MissedPaymentsCount = await GetMissedPaymentsCountAsync(userId),
                    RequestedAmount = requestedAmount,
                    Notes = note
                };

            var result = await CalculateAndPersistAsync(userId, dto);
            return new CreditScoreDecisionContext
            {
                Result = result,
                CanProceed = result.Decision != CreditDecision.Rejected.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evaluate application credit score failed");
            return new CreditScoreDecisionContext
            {
                Result = new CreditScoreResultDto
                {
                    UserId = userId,
                    Decision = CreditDecision.ManualReview.ToString(),
                    Explanation = "Credit score evaluation could not complete automatically."
                },
                CanProceed = true
            };
        }
    }

    private async Task<CreditScoreResultDto> CalculateAndPersistAsync(Guid userId, CreditScoreCalculateDto dto)
    {
        var currentLoanCount = await GetCurrentLoanCountAsync(userId);
        var accountTurnover = await GetAccountTurnoverAsync(userId);
        var contributions = new List<string>();
        var score = 600;

        if (dto.MonthlyIncome >= 10000)
        {
            score += 120;
            contributions.Add("High income improved the score.");
        }
        else if (dto.MonthlyIncome >= 5000)
        {
            score += 80;
            contributions.Add("Stable income improved the score.");
        }
        else if (dto.MonthlyIncome >= 2000)
        {
            score += 40;
            contributions.Add("Moderate income added positive weight.");
        }
        else
        {
            score += 10;
            contributions.Add("Low income limited the score improvement.");
        }

        var employment = dto.EmploymentStatus.Trim().ToLowerInvariant();
        if (employment.Contains("government") || employment.Contains("permanent") || employment.Contains("full"))
        {
            score += 80;
            contributions.Add("Stable employment improved the score.");
        }
        else if (employment.Contains("contract") || employment.Contains("self"))
        {
            score += 40;
            contributions.Add("Moderately stable employment improved the score.");
        }
        else if (employment.Contains("unemployed"))
        {
            score -= 120;
            contributions.Add("Unemployment reduced the score.");
        }

        if (dto.ExistingDebt == 0)
        {
            score += 40;
            contributions.Add("No existing debt improved the score.");
        }
        else if (dto.ExistingDebt > dto.MonthlyIncome * 10)
        {
            score -= 140;
            contributions.Add("Existing debt is too high relative to income.");
        }
        else if (dto.ExistingDebt > dto.MonthlyIncome * 5)
        {
            score -= 80;
            contributions.Add("Existing debt significantly reduced the score.");
        }
        else if (dto.ExistingDebt > dto.MonthlyIncome * 2)
        {
            score -= 40;
            contributions.Add("Existing debt moderately reduced the score.");
        }

        if (dto.CreditHistoryLengthMonths >= 60)
        {
            score += 70;
            contributions.Add("Long credit history improved the score.");
        }
        else if (dto.CreditHistoryLengthMonths >= 24)
        {
            score += 40;
            contributions.Add("Good credit history improved the score.");
        }
        else if (dto.CreditHistoryLengthMonths >= 12)
        {
            score += 20;
            contributions.Add("Basic credit history improved the score slightly.");
        }
        else if (dto.CreditHistoryLengthMonths < 6)
        {
            score -= 40;
            contributions.Add("Very short credit history reduced the score.");
        }

        if (dto.MissedPaymentsCount == 0)
        {
            score += 60;
            contributions.Add("No missed payments improved the score.");
        }
        else if (dto.MissedPaymentsCount == 1)
        {
            score -= 20;
            contributions.Add("One missed payment reduced the score.");
        }
        else if (dto.MissedPaymentsCount == 2)
        {
            score -= 60;
            contributions.Add("Two missed payments reduced the score significantly.");
        }
        else
        {
            score -= 120;
            contributions.Add("Repeated missed payments reduced the score heavily.");
        }

        if (currentLoanCount == 0)
        {
            score += 40;
            contributions.Add("No active loans improved the score.");
        }
        else if (currentLoanCount == 1)
        {
            score += 10;
            contributions.Add("Manageable active loan count kept the score stable.");
        }
        else if (currentLoanCount == 2)
        {
            score -= 25;
            contributions.Add("Multiple active loans reduced the score.");
        }
        else
        {
            score -= 70;
            contributions.Add("Too many active loans reduced the score heavily.");
        }

        if (accountTurnover >= dto.MonthlyIncome * 6)
        {
            score += 60;
            contributions.Add("Strong account turnover improved the score.");
        }
        else if (accountTurnover >= dto.MonthlyIncome * 3)
        {
            score += 30;
            contributions.Add("Healthy account turnover improved the score.");
        }
        else if (accountTurnover < dto.MonthlyIncome)
        {
            score -= 20;
            contributions.Add("Low account turnover reduced the score.");
        }

        if (dto.RequestedAmount.HasValue)
        {
            if (dto.RequestedAmount.Value > dto.MonthlyIncome * 20)
            {
                score -= 80;
                contributions.Add("Requested amount is very high relative to income.");
            }
            else if (dto.RequestedAmount.Value > dto.MonthlyIncome * 10)
            {
                score -= 40;
                contributions.Add("Requested amount is high relative to income.");
            }
        }

        score = Math.Clamp(score, 300, 900);
        var decision = score >= 720 ? CreditDecision.Approved : score >= 600 ? CreditDecision.ManualReview : CreditDecision.Rejected;
        var explanation = string.Join(" ", contributions);

        var profile = await _db.CreditScoreProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (profile == null)
        {
            profile = new CreditScoreProfile { UserId = userId };
            _db.CreditScoreProfiles.Add(profile);
        }

        profile.MonthlyIncome = dto.MonthlyIncome;
        profile.EmploymentStatus = dto.EmploymentStatus;
        profile.ExistingDebt = dto.ExistingDebt;
        profile.CreditHistoryLengthMonths = dto.CreditHistoryLengthMonths;
        profile.MissedPaymentsCount = dto.MissedPaymentsCount;
        profile.CurrentLoanCount = currentLoanCount;
        profile.AccountTurnover = accountTurnover;
        profile.ScoreValue = score;
        profile.Decision = decision;
        profile.CalculatedAt = DateTime.UtcNow;
        profile.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? explanation : $"{dto.Notes} {explanation}".Trim();

        _db.CreditScoreHistory.Add(new CreditScoreHistory
        {
            UserId = userId,
            MonthlyIncome = profile.MonthlyIncome,
            EmploymentStatus = profile.EmploymentStatus,
            ExistingDebt = profile.ExistingDebt,
            CreditHistoryLengthMonths = profile.CreditHistoryLengthMonths,
            MissedPaymentsCount = profile.MissedPaymentsCount,
            CurrentLoanCount = profile.CurrentLoanCount,
            AccountTurnover = profile.AccountTurnover,
            ScoreValue = profile.ScoreValue,
            Decision = profile.Decision,
            CalculatedAt = profile.CalculatedAt,
            Notes = profile.Notes
        });

        await _db.SaveChangesAsync();
        return MapToDto(profile);
    }

    private async Task<CreditScoreCalculateDto> BuildDerivedInputAsync(Guid userId, decimal requestedAmount, string note)
    {
        var monthlyIncome = await _db.Transactions.AsNoTracking()
            .Where(x => x.Status == TransactionStatus.Completed
                        && x.ToAccount != null
                        && x.ToAccount.UserId == userId
                        && x.CreatedAt >= DateTime.UtcNow.AddDays(-90))
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        monthlyIncome = monthlyIncome > 0 ? Math.Round(monthlyIncome / 3, 2) : 1500m;

        return new CreditScoreCalculateDto
        {
            MonthlyIncome = monthlyIncome,
            EmploymentStatus = "Unknown",
            ExistingDebt = await GetExistingDebtAsync(userId),
            CreditHistoryLengthMonths = await GetCreditHistoryLengthMonthsAsync(userId),
            MissedPaymentsCount = await GetMissedPaymentsCountAsync(userId),
            RequestedAmount = requestedAmount,
            Notes = note
        };
    }

    private async Task<int> GetCurrentLoanCountAsync(Guid userId)
    {
        var loans = await _db.Loans.AsNoTracking().CountAsync(x => x.UserId == userId && x.Status == LoanStatus.Active);
        var autoCredits = await _db.AutoCredits.AsNoTracking().CountAsync(x => x.UserId == userId && x.Status == LoanStatus.Active);
        return loans + autoCredits;
    }

    private async Task<decimal> GetExistingDebtAsync(Guid userId)
    {
        var loans = await _db.Loans.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == LoanStatus.Active)
            .SumAsync(x => (decimal?)x.RemainingAmount) ?? 0m;
        var autoCredits = await _db.AutoCredits.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == LoanStatus.Active)
            .SumAsync(x => (decimal?)x.RemainingAmount) ?? 0m;
        return loans + autoCredits;
    }

    private async Task<decimal> GetAccountTurnoverAsync(Guid userId)
    {
        var accountIds = await _db.Accounts.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        return await _db.Transactions.AsNoTracking()
            .Where(x => x.Status == TransactionStatus.Completed
                        && x.CreatedAt >= DateTime.UtcNow.AddDays(-90)
                        && ((x.FromAccountId.HasValue && accountIds.Contains(x.FromAccountId.Value))
                            || (x.ToAccountId.HasValue && accountIds.Contains(x.ToAccountId.Value))))
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;
    }

    private async Task<int> GetCreditHistoryLengthMonthsAsync(Guid userId)
    {
        var user = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == userId);
        var firstTransaction = await _db.Transactions.AsNoTracking()
            .Where(x => (x.FromAccount != null && x.FromAccount.UserId == userId)
                        || (x.ToAccount != null && x.ToAccount.UserId == userId))
            .OrderBy(x => x.CreatedAt)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefaultAsync();

        var startDate = firstTransaction ?? user.CreatedAt;
        return Math.Max(0, ((DateTime.UtcNow.Year - startDate.Year) * 12) + DateTime.UtcNow.Month - startDate.Month);
    }

    private async Task<int> GetMissedPaymentsCountAsync(Guid userId)
    {
        var overdueLoans = await _db.Loans.AsNoTracking().CountAsync(x => x.UserId == userId && x.Status == LoanStatus.Overdue);
        var overdueAutoCredits = await _db.AutoCredits.AsNoTracking().CountAsync(x => x.UserId == userId && x.Status == LoanStatus.Overdue);
        return overdueLoans + overdueAutoCredits;
    }

    private static CreditScoreResultDto MapToDto(CreditScoreProfile profile) => new()
    {
        UserId = profile.UserId,
        MonthlyIncome = profile.MonthlyIncome,
        EmploymentStatus = profile.EmploymentStatus,
        ExistingDebt = profile.ExistingDebt,
        CreditHistoryLengthMonths = profile.CreditHistoryLengthMonths,
        MissedPaymentsCount = profile.MissedPaymentsCount,
        CurrentLoanCount = profile.CurrentLoanCount,
        AccountTurnover = profile.AccountTurnover,
        ScoreValue = profile.ScoreValue,
        Decision = profile.Decision.ToString(),
        CalculatedAt = profile.CalculatedAt,
        Notes = profile.Notes,
        Explanation = profile.Notes ?? string.Empty
    };

    private static CreditScoreResultDto MapToDto(CreditScoreHistory profile) => new()
    {
        UserId = profile.UserId,
        MonthlyIncome = profile.MonthlyIncome,
        EmploymentStatus = profile.EmploymentStatus,
        ExistingDebt = profile.ExistingDebt,
        CreditHistoryLengthMonths = profile.CreditHistoryLengthMonths,
        MissedPaymentsCount = profile.MissedPaymentsCount,
        CurrentLoanCount = profile.CurrentLoanCount,
        AccountTurnover = profile.AccountTurnover,
        ScoreValue = profile.ScoreValue,
        Decision = profile.Decision.ToString(),
        CalculatedAt = profile.CalculatedAt,
        Notes = profile.Notes,
        Explanation = profile.Notes ?? string.Empty
    };
}
