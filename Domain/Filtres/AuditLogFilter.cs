namespace SomoniBank.Domain.Filtres;

public class AuditLogFilter
{
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}