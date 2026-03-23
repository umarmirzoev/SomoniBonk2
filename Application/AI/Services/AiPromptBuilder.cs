using System.Globalization;
using System.Text;
using SomoniBank.Application.AI.DTOs;
using SomoniBank.Application.AI.Interfaces;

namespace SomoniBank.Application.AI.Services;

public class AiPromptBuilder : IAiPromptBuilder
{
    public string BuildFinancialAssistantPrompt(AiAskRequestDto request, AiContextDto context)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You are SomoniBank AI, a banking financial assistant inside SomoniBank.");
        builder.AppendLine("Stay in the banking assistant role at all times.");
        builder.AppendLine("Answer clearly, briefly, and use only the banking data provided below.");
        builder.AppendLine("Do not invent balances, transactions, accounts, exchange rates, or customer facts.");
        builder.AppendLine("If some data is missing, explicitly say that the data is not available.");
        builder.AppendLine("Do not promise profit, approval, guaranteed savings, or any unsafe financial outcome.");
        builder.AppendLine("Do not answer as a generic chatbot.");
        builder.AppendLine("Support English, Russian, or Tajik depending on the requested language.");
        builder.AppendLine();

        builder.AppendLine($"Preferred response language: {ResolveLanguage(request.Language)}");
        builder.AppendLine($"Customer name: {ValueOrNotAvailable(context.UserFullName)}");
        builder.AppendLine($"Total balance: {FormatMoney(context.TotalBalance)}");
        builder.AppendLine($"Currency summary: {ValueOrNotAvailable(context.CurrencySummary)}");
        builder.AppendLine();

        builder.AppendLine("Accounts:");
        AppendList(builder, context.Accounts);
        builder.AppendLine();

        builder.AppendLine("Recent transactions:");
        AppendList(builder, context.RecentTransactions);
        builder.AppendLine();

        builder.AppendLine("Exchange rates:");
        AppendList(builder, context.ExchangeRates);
        builder.AppendLine();

        builder.AppendLine("User question:");
        builder.AppendLine(request.UserQuestion.Trim());
        builder.AppendLine();
        builder.AppendLine("Answer with banking context only. If exact calculation is impossible from the given data, say that the available data is insufficient.");
        builder.AppendLine("If balances are in different currencies, rely on the currency summary and avoid pretending that raw totals are FX-normalized.");

        return builder.ToString();
    }

    private static void AppendList(StringBuilder builder, IEnumerable<string>? items)
    {
        var normalizedItems = items?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (normalizedItems is null || normalizedItems.Count == 0)
        {
            builder.AppendLine("- Not available");
            return;
        }

        foreach (var item in normalizedItems)
        {
            builder.AppendLine($"- {item}");
        }
    }

    private static string ResolveLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "Use the same language as the user's question when possible";

        var normalized = language.Trim().ToLowerInvariant();

        return normalized switch
        {
            "ru" or "russian" => "Russian",
            "tg" or "tajik" => "Tajik",
            "en" or "english" => "English",
            _ => "Use the requested language if possible"
        };
    }

    private static string FormatMoney(decimal? amount)
        => amount.HasValue
            ? amount.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "Not available";

    private static string ValueOrNotAvailable(string? value)
        => string.IsNullOrWhiteSpace(value) ? "Not available" : value.Trim();
}
