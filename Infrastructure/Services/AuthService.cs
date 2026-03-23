using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, IFraudDetectionService fraudDetectionService, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    public async Task<Response<string>> RegisterAsync(UserInsertDto dto)
    {
        try
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return new Response<string>(HttpStatusCode.BadRequest, "Email already exists");

            if (await _db.Users.AnyAsync(u => u.PassportNumber == dto.PassportNumber))
                return new Response<string>(HttpStatusCode.BadRequest, "Passport number already registered");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                Address = dto.Address,
                PassportNumber = dto.PassportNumber,
                Role = UserRole.Client
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Registration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            var isSuccess = user != null && BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (user != null)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsSuccess = isSuccess
                });
                await _db.SaveChangesAsync();
            }

            if (!isSuccess)
            {
                await _fraudDetectionService.ProcessFailedLoginAsync(dto.Email, ipAddress, userAgent);
                return new Response<AuthResponseDto>(HttpStatusCode.Unauthorized, "Invalid email or password");
            }

            if (!user!.IsActive)
                return new Response<AuthResponseDto>(HttpStatusCode.Forbidden, "Account is blocked");

            var result = new AuthResponseDto
            {
                Token = GenerateToken(user),
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}"
            };

            return new Response<AuthResponseDto>(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return new Response<AuthResponseDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<AuthResponseDto>> CreatePinAsync(CreatePinRequestDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var normalizedPhone = NormalizePhone(dto.Phone);
            if (!IsValidPin(dto.Pin))
                return new Response<AuthResponseDto>(HttpStatusCode.BadRequest, "PIN must be 4-6 digits");

            if (!IsValidPhoneVerificationToken(normalizedPhone, dto.VerificationToken))
                return new Response<AuthResponseDto>(HttpStatusCode.Unauthorized, "Phone verification is required");

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Phone == normalizedPhone);
            if (user == null)
            {
                user = new User
                {
                    FirstName = "Somoni",
                    LastName = "Client",
                    Email = BuildDevelopmentEmail(normalizedPhone),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(16))),
                    PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin),
                    Phone = normalizedPhone,
                    Address = "Phone onboarding",
                    PassportNumber = BuildDevelopmentPassport(normalizedPhone),
                    Role = UserRole.Client,
                    IsActive = true
                };

                _db.Users.Add(user);
            }
            else
            {
                user.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            }

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "CreatePin",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = true
            });

            await _db.SaveChangesAsync();

            var result = new AuthResponseDto
            {
                Token = GenerateToken(user),
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}"
            };

            return new Response<AuthResponseDto>(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreatePin failed");
            return new Response<AuthResponseDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<AuthResponseDto>> LoginWithPinAsync(PinLoginRequestDto dto, string ipAddress, string userAgent)
    {
        try
        {
            var normalizedPhone = NormalizePhone(dto.Phone);
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Phone == normalizedPhone);

            var isSuccess = user != null
                && !string.IsNullOrWhiteSpace(user.PinHash)
                && BCrypt.Net.BCrypt.Verify(dto.Pin, user.PinHash);

            if (user != null)
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "PinLogin",
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsSuccess = isSuccess
                });
                await _db.SaveChangesAsync();
            }

            if (!isSuccess)
                return new Response<AuthResponseDto>(HttpStatusCode.Unauthorized, "Invalid phone or PIN");

            if (!user!.IsActive)
                return new Response<AuthResponseDto>(HttpStatusCode.Forbidden, "Account is blocked");

            var result = new AuthResponseDto
            {
                Token = GenerateToken(user),
                Role = user.Role.ToString(),
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}"
            };

            return new Response<AuthResponseDto>(HttpStatusCode.OK, "Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pin login failed");
            return new Response<AuthResponseDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        try
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return new Response<string>(HttpStatusCode.BadRequest, "Passwords do not match");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, "User not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                return new Response<string>(HttpStatusCode.BadRequest, "Old password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangePassword failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FullName", $"{user.FirstName} {user.LastName}")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GeneratePhoneVerificationToken(string phone)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim("purpose", "phone_verification"),
            new Claim("phone", NormalizePhone(phone))
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool IsValidPhoneVerificationToken(string phone, string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out _);

            var purpose = principal.FindFirst("purpose")?.Value;
            var tokenPhone = principal.FindFirst("phone")?.Value;

            return purpose == "phone_verification"
                && string.Equals(tokenPhone, NormalizePhone(phone), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPin(string pin)
        => !string.IsNullOrWhiteSpace(pin)
           && pin.Length is >= 4 and <= 6
           && pin.All(char.IsDigit);

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var normalized = phone.Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty);

        if (normalized.StartsWith("00", StringComparison.Ordinal))
            normalized = "+" + normalized[2..];

        return normalized;
    }

    private static string BuildDevelopmentEmail(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return $"phone-{digits}@dev.somonibank.local";
    }

    private static string BuildDevelopmentPassport(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return $"DEV{digits[..Math.Min(digits.Length, 17)]}";
    }
}
