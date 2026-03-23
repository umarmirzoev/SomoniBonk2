using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class KycProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string NationalIdNumber { get; set; } = null!;
    public string PassportNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string SelfieImageUrl { get; set; } = null!;
    public string DocumentFrontUrl { get; set; } = null!;
    public string DocumentBackUrl { get; set; } = null!;
    public KycStatus Status { get; set; } = KycStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? RejectionReason { get; set; }

    public User User { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
    public ICollection<KycDocument> Documents { get; set; } = new List<KycDocument>();
}
