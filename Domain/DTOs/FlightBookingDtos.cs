namespace SomoniBank.Domain.DTOs;

public class FlightBookingInsertDto
{
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
    public string Currency { get; set; } = null!;
}

public class FlightBookingGetDto
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = null!;
    public string FromCity { get; set; } = null!;
    public string ToCity { get; set; } = null!;
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int PassengerCount { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsInstallment { get; set; }
    public decimal? MonthlyPayment { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
