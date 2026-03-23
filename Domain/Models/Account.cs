using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public AccountType Type { get; set; }
    public Currency Currency { get; set; }
    public decimal Balance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
    public ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();
    public ICollection<InternationalTransfer> InternationalTransfers { get; set; } = new List<InternationalTransfer>();
    public ICollection<SavingsGoal> SavingsGoals { get; set; } = new List<SavingsGoal>();
    public ICollection<QrPayment> ReceivedQrPayments { get; set; } = new List<QrPayment>();
    public ICollection<AutoCredit> AutoCredits { get; set; } = new List<AutoCredit>();
    public ICollection<FlightBooking> FlightBookings { get; set; } = new List<FlightBooking>();
    public ICollection<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
    public ICollection<VirtualCard> VirtualCards { get; set; } = new List<VirtualCard>();
    public TransactionLimit? TransactionLimit { get; set; }
}
