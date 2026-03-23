using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ICreditScoringService
{
    Task<Response<CreditScoreResultDto>> CalculateAsync(Guid userId, CreditScoreCalculateDto dto);
    Task<Response<CreditScoreResultDto>> GetLatestAsync(Guid userId);
    Task<Response<CreditScoreResultDto>> GetLatestForAdminAsync(Guid userId);
    Task<Response<List<CreditScoreResultDto>>> GetHistoryAsync(Guid userId);
    Task<CreditScoreDecisionContext> EvaluateApplicationAsync(Guid userId, decimal requestedAmount, string note);
}

public sealed class CreditScoreDecisionContext
{
    public CreditScoreResultDto Result { get; init; } = null!;
    public bool CanProceed { get; init; }
}
