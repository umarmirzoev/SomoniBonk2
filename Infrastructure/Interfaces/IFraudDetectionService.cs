using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Filtres;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IFraudDetectionService
{
    Task<FraudEvaluationResult> EvaluateTransferAsync(Guid userId, Account fromAccount, decimal amount, string? description);
    Task<FraudEvaluationResult> EvaluateBillPaymentAsync(Guid userId, Account account, decimal amount, string providerName);
    Task<FraudEvaluationResult> EvaluateInternationalTransferAsync(Guid userId, Account fromAccount, decimal amount, string country);
    Task ProcessFailedLoginAsync(string email, string ipAddress, string userAgent);
    Task<Response<string>> ReportBlockedCardUsageAsync(Guid userId, string cardReference, string source, string? notes);
    Task<PagedResult<FraudAlertGetDto>> GetAllAsync(FraudAlertFilter filter, PagedQuery pagedQuery);
    Task<Response<FraudAlertGetDto>> GetByIdAsync(Guid id);
    Task<Response<string>> ReviewAsync(Guid id, Guid adminId, FraudAlertReviewDto dto);
    Task<Response<string>> BlockAsync(Guid id, Guid adminId, FraudAlertReviewDto dto);
    Task<Response<string>> IgnoreAsync(Guid id, Guid adminId, FraudAlertReviewDto dto);
}

public sealed class FraudEvaluationResult
{
    public bool IsBlocked { get; init; }
    public int RiskScore { get; init; }
    public RiskLevel RiskLevel { get; init; }
    public string Reason { get; init; } = "";
    public Guid? FraudAlertId { get; init; }
}
