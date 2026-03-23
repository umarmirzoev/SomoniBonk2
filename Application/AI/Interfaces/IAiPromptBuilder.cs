using SomoniBank.Application.AI.DTOs;

namespace SomoniBank.Application.AI.Interfaces;

public interface IAiPromptBuilder
{
    string BuildFinancialAssistantPrompt(AiAskRequestDto request, AiContextDto context);
}
