using SomoniBank.Application.AI.DTOs;

namespace SomoniBank.Application.AI.Interfaces;

public interface IAiContextService
{
    Task<AiContextDto> BuildContextAsync(CancellationToken cancellationToken = default);
}
