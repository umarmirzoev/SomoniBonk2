using System.ComponentModel.DataAnnotations;

namespace SomoniBank.Domain.DTOs;

public class SendCodeRequestDto
{
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string Phone { get; set; } = null!;
}

public class VerifyCodeRequestDto
{
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string Phone { get; set; } = null!;

    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string Code { get; set; } = null!;
}

public class SendCodeResponseDto
{
    public bool Success { get; set; }
    public string? Code { get; set; }
    public int ExpiresInSeconds { get; set; }
}

public class VerifyResult
{
    public bool Success { get; set; }
    public bool ExistingUser { get; set; }
    public string? VerificationToken { get; set; }
    public string? Error { get; set; }
}

public class CreatePinRequestDto
{
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string Phone { get; set; } = null!;

    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string Pin { get; set; } = null!;

    [Required]
    public string VerificationToken { get; set; } = null!;
}

public class PinLoginRequestDto
{
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string Phone { get; set; } = null!;

    [Required]
    [StringLength(6, MinimumLength = 4)]
    public string Pin { get; set; } = null!;
}
