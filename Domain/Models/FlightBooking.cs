using SomoniBank.Domain.Enums;

namespace SomoniBank.Domain.Models;

public class FlightBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string FlightNumber { get; set; } = null!;
    public string FromCity { get; set; } = null!;
    public string ToCity { get; set; } = null!;
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int PassengerCount { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsInstallment { get; set; }
    public int? InstallmentMonths { get; set; }
    public decimal? MonthlyPayment { get; set; }
    public Currency Currency { get; set; }
    public string Status { get; set; } = "Confirmed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
