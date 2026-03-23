using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class KycDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid KycProfileId { get; set; }
    public KycDocumentType Type { get; set; }
    public string FileUrl { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public KycProfile KycProfile { get; set; } = null!;
}
