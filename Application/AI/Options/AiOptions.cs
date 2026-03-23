namespace SomoniBank.Application.AI.Options;

public class AiOptions
{
    public const string SectionName = "Ai";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.2d;
    public int MaxTokens { get; set; } = 512;
}
