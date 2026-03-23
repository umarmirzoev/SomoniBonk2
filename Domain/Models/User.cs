using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PinHash { get; set; }
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string PassportNumber { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.Client;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Cashback> Cashbacks { get; set; } = new List<Cashback>();
    public ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();
    public ICollection<InternationalTransfer> InternationalTransfers { get; set; } = new List<InternationalTransfer>();
    public ICollection<SavingsGoal> SavingsGoals { get; set; } = new List<SavingsGoal>();
    public ICollection<QrPayment> GeneratedQrPayments { get; set; } = new List<QrPayment>();
    public ICollection<AutoCredit> AutoCredits { get; set; } = new List<AutoCredit>();
    public ICollection<FlightBooking> FlightBookings { get; set; } = new List<FlightBooking>();
    public KycProfile? KycProfile { get; set; }
    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    public ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
    public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();
    public ICollection<FraudAlert> FraudAlerts { get; set; } = new List<FraudAlert>();
    public ICollection<VirtualCard> VirtualCards { get; set; } = new List<VirtualCard>();
    public ICollection<CreditScoreProfile> CreditScoreProfiles { get; set; } = new List<CreditScoreProfile>();
    public ICollection<CreditScoreHistory> CreditScoreHistory { get; set; } = new List<CreditScoreHistory>();
}
