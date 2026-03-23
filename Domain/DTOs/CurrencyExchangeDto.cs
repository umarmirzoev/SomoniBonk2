namespace SomoniBank.Domain.DTOs;

public class CurrencyExchangeDto
{
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public decimal Amount { get; set; }
}   