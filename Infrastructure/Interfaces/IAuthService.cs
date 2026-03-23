using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IAuthService
{
    Task<Response<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress, string userAgent);
    Task<Response<AuthResponseDto>> CreatePinAsync(CreatePinRequestDto dto, string ipAddress, string userAgent);
    Task<Response<AuthResponseDto>> LoginWithPinAsync(PinLoginRequestDto dto, string ipAddress, string userAgent);
    Task<Response<string>> RegisterAsync(UserInsertDto dto);
    Task<Response<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    string GenerateToken(SomoniBank.Domain.Models.User user);
    string GeneratePhoneVerificationToken(string phone);
    bool IsValidPhoneVerificationToken(string phone, string token);
}
