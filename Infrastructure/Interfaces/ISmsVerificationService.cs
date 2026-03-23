using SomoniBank.Domain.DTOs;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ISmsVerificationService
{
    Task<SendCodeResponseDto> SendCodeAsync(string phone, CancellationToken cancellationToken = default);
    Task<VerifyResult> VerifyCodeAsync(string phone, string code, CancellationToken cancellationToken = default);
}
