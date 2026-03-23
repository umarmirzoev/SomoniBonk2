namespace SomoniBank.Application.AI.DTOs;

public class AiAskResponseDto
{
    public bool Success { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string? Error { get; set; }
}
