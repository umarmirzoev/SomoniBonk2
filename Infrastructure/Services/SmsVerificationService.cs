using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.Infrastructure.Services;

public class SmsVerificationService(
    AppDbContext db,
    ISmsSender smsSender,
    IAuthService authService,
    IHostEnvironment hostEnvironment,
    ILogger<SmsVerificationService> logger) : ISmsVerificationService
{
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);
    private const int CodeLifetimeSeconds = 60;
    private const int VerificationCodeLength = 5;

    public async Task<SendCodeResponseDto> SendCodeAsync(string phone, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhone(phone);
        if (!IsValidPhone(normalizedPhone))
        {
            return new SendCodeResponseDto { Success = false, ExpiresInSeconds = CodeLifetimeSeconds };
        }

        var now = DateTime.UtcNow;
        var activeCodes = await db.SmsCodes
            .Where(x => x.Phone == normalizedPhone && !x.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var activeCode in activeCodes)
        {
            activeCode.IsUsed = true;
        }

        var code = GenerateCode();
        var verificationCode = new SmsCode
        {
            Phone = normalizedPhone,
            Code = code,
            ExpiresAt = now.AddSeconds(CodeLifetimeSeconds),
            IsUsed = false,
            CreatedAt = now
        };

        db.SmsCodes.Add(verificationCode);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await smsSender.SendAsync(
                normalizedPhone,
                $"Your code is {code}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification code to {Phone}", normalizedPhone);

            verificationCode.IsUsed = true;
            await db.SaveChangesAsync(cancellationToken);

            return new SendCodeResponseDto { Success = false, ExpiresInSeconds = CodeLifetimeSeconds };
        }

        logger.LogInformation("Verification code created for {Phone}, expires at {ExpiresAt}", normalizedPhone, verificationCode.ExpiresAt);

        return new SendCodeResponseDto
        {
            Success = true,
            Code = hostEnvironment.IsDevelopment() ? code : null,
            ExpiresInSeconds = CodeLifetimeSeconds
        };
    }

    public async Task<VerifyResult> VerifyCodeAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhone(phone);
        var normalizedCode = code?.Trim() ?? string.Empty;

        if (!IsValidPhone(normalizedPhone) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new VerifyResult
            {
                Success = false,
                Error = "invalid"
            };
        }

        var verificationCode = await db.SmsCodes
            .Where(x => x.Phone == normalizedPhone && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (verificationCode == null)
        {
            return new VerifyResult
            {
                Success = false,
                Error = "invalid"
            };
        }

        if (verificationCode.ExpiresAt <= DateTime.UtcNow)
        {
            verificationCode.IsUsed = true;
            await db.SaveChangesAsync(cancellationToken);

            return new VerifyResult
            {
                Success = false,
                Error = "expired"
            };
        }

        if (!string.Equals(verificationCode.Code, normalizedCode, StringComparison.Ordinal))
        {
            return new VerifyResult
            {
                Success = false,
                Error = "invalid"
            };
        }

        verificationCode.IsUsed = true;
        await db.SaveChangesAsync(cancellationToken);

        var existingUser = await db.Users.AnyAsync(x => x.Phone == normalizedPhone, cancellationToken);

        return new VerifyResult
        {
            Success = true,
            ExistingUser = existingUser,
            VerificationToken = authService.GeneratePhoneVerificationToken(normalizedPhone)
        };
    }

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
        {
            normalized = "+" + normalized[2..];
        }

        return normalized;
    }

    private static bool IsValidPhone(string phone) => PhoneRegex.IsMatch(phone);

    private static string GenerateCode()
    {
        var minValue = (int)Math.Pow(10, VerificationCodeLength - 1);
        var maxValue = (int)Math.Pow(10, VerificationCodeLength);
        return RandomNumberGenerator.GetInt32(minValue, maxValue).ToString();
    }
}
