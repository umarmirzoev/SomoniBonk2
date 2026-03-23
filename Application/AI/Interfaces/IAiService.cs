using SomoniBank.Application.AI.DTOs;

namespace SomoniBank.Application.AI.Interfaces;

public interface IAiService
{
    Task<AiAskResponseDto> AskAsync(AiAskRequestDto request, AiContextDto context, CancellationToken cancellationToken = default);
}
