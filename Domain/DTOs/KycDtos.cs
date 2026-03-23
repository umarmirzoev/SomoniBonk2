namespace SomoniBank.Domain.DTOs;

public class KycSubmitDto
{
    public string FullName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string NationalIdNumber { get; set; } = null!;
    public string PassportNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string SelfieImageUrl { get; set; } = null!;
    public string DocumentFrontUrl { get; set; } = null!;
    public string DocumentBackUrl { get; set; } = null!;
}

public class KycUpdateDto
{
    public string FullName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string NationalIdNumber { get; set; } = null!;
    public string PassportNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string SelfieImageUrl { get; set; } = null!;
    public string DocumentFrontUrl { get; set; } = null!;
    public string DocumentBackUrl { get; set; } = null!;
}

public class KycReviewDto
{
    public string? RejectionReason { get; set; }
}

public class KycProfileGetDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string NationalIdNumber { get; set; } = null!;
    public string PassportNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string SelfieImageUrl { get; set; } = null!;
    public string DocumentFrontUrl { get; set; } = null!;
    public string DocumentBackUrl { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }
}

public class KycStatusGetDto
{
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}
