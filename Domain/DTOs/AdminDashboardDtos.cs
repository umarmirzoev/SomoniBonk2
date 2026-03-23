namespace SomoniBank.Domain.DTOs;

public class AdminDashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int TotalAccounts { get; set; }
    public int TotalCards { get; set; }
    public int TotalVirtualCards { get; set; }
    public int TotalDeposits { get; set; }
    public int TotalLoans { get; set; }
    public decimal TotalTransfersVolume { get; set; }
    public int PendingKycRequests { get; set; }
    public int OpenFraudAlerts { get; set; }
    public int OpenSupportTickets { get; set; }
    public string SystemHealthSummary { get; set; } = null!;
    public List<TopActiveUserDto> TopActiveUsers { get; set; } = [];
}

public class TopActiveUserDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public int TransactionCount { get; set; }
    public decimal TransactionVolume { get; set; }
}

public class RecentTransactionAdminDto
{
    public Guid Id { get; set; }
    public string? FromAccountNumber { get; set; }
    public string? ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class RecentLoginDto
{
    public Guid AuditLogId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public bool IsSuccess { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingKycDashboardDto
{
    public Guid KycProfileId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = null!;
}

public class OpenFraudAlertDashboardDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = null!;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class SupportTicketDashboardDto
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Priority { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LoanOverviewDto
{
    public int PendingLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int PaidLoans { get; set; }
    public int RejectedLoans { get; set; }
    public int PendingAutoCredits { get; set; }
    public int ActiveAutoCredits { get; set; }
    public int PaidAutoCredits { get; set; }
    public int RejectedAutoCredits { get; set; }
}
