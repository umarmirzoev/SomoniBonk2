namespace SomoniBank.Application.AI.DTOs;

public class AiContextDto
{
    public string? UserFullName { get; set; }
    public decimal TotalBalance { get; set; }
    public List<string> Accounts { get; set; } = [];
    public List<string> RecentTransactions { get; set; } = [];
    public List<string> ExchangeRates { get; set; } = [];
    public string CurrencySummary { get; set; } = string.Empty;
}
