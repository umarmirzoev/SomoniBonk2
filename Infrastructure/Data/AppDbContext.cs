using Microsoft.EntityFrameworkCore;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Configurations;

namespace SomoniBank.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<SmsCode> SmsCodes { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Deposit> Deposits { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<CurrencyRate> CurrencyRates { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<TransactionLimit> TransactionLimits { get; set; }
    public DbSet<Installment> Installments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Cashback> Cashbacks { get; set; }
    public DbSet<BillCategory> BillCategories { get; set; }
    public DbSet<BillProvider> BillProviders { get; set; }
    public DbSet<BillPayment> BillPayments { get; set; }
    public DbSet<InternationalTransfer> InternationalTransfers { get; set; }
    public DbSet<SavingsGoal> SavingsGoals { get; set; }
    public DbSet<QrPayment> QrPayments { get; set; }
    public DbSet<AutoCredit> AutoCredits { get; set; }
    public DbSet<FlightBooking> FlightBookings { get; set; }
    public DbSet<KycProfile> KycProfiles { get; set; }
    public DbSet<KycDocument> KycDocuments { get; set; }
    public DbSet<Beneficiary> Beneficiaries { get; set; }
    public DbSet<RecurringPayment> RecurringPayments { get; set; }
    public DbSet<RecurringPaymentHistory> RecurringPaymentHistory { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<SupportMessage> SupportMessages { get; set; }
    public DbSet<FraudAlert> FraudAlerts { get; set; }
    public DbSet<VirtualCard> VirtualCards { get; set; }
    public DbSet<CreditScoreProfile> CreditScoreProfiles { get; set; }
    public DbSet<CreditScoreHistory> CreditScoreHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new SmsCodeConfiguration());
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new DepositConfiguration());
        modelBuilder.ApplyConfiguration(new LoanConfiguration());
        modelBuilder.ApplyConfiguration(new CardConfiguration());
        modelBuilder.ApplyConfiguration(new CurrencyRateConfiguration());
        modelBuilder.ApplyConfiguration(new ExchangeRateConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionLimitConfiguration());
        modelBuilder.ApplyConfiguration(new InstallmentConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
        modelBuilder.ApplyConfiguration(new CashbackConfiguration());
        modelBuilder.ApplyConfiguration(new BillCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new BillProviderConfiguration());
        modelBuilder.ApplyConfiguration(new BillPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new InternationalTransferConfiguration());
        modelBuilder.ApplyConfiguration(new SavingsGoalConfiguration());
        modelBuilder.ApplyConfiguration(new QrPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new AutoCreditConfiguration());
        modelBuilder.ApplyConfiguration(new FlightBookingConfiguration());
        modelBuilder.ApplyConfiguration(new KycProfileConfiguration());
        modelBuilder.ApplyConfiguration(new KycDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new BeneficiaryConfiguration());
        modelBuilder.ApplyConfiguration(new RecurringPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new RecurringPaymentHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new SupportTicketConfiguration());
        modelBuilder.ApplyConfiguration(new SupportMessageConfiguration());
        modelBuilder.ApplyConfiguration(new FraudAlertConfiguration());
        modelBuilder.ApplyConfiguration(new VirtualCardConfiguration());
        modelBuilder.ApplyConfiguration(new CreditScoreProfileConfiguration());
        modelBuilder.ApplyConfiguration(new CreditScoreHistoryConfiguration());
    }
}
