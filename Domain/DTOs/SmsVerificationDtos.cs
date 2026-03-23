namespace SomoniBank.Domain.DTOs;

public class SendCodeRequestDto
{
    public string Phone { get; set; } = null!;
}

public class VerifyCodeRequestDto
{
    public string Phone { get; set; } = null!;
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
    public string Phone { get; set; } = null!;
    public string Pin { get; set; } = null!;
    public string VerificationToken { get; set; } = null!;
}

public class PinLoginRequestDto
{
    public string Phone { get; set; } = null!;
    public string Pin { get; set; } = null!;
}
