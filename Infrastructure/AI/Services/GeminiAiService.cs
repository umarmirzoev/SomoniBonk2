using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SomoniBank.Application.AI.DTOs;
using SomoniBank.Application.AI.Interfaces;
using SomoniBank.Application.AI.Options;

namespace SomoniBank.Infrastructure.AI.Services;

public class GeminiAiService : IAiService
{
    private const string SafeErrorMessage = "AI assistant is temporarily unavailable. Please try again later.";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IAiPromptBuilder _promptBuilder;
    private readonly AiOptions _options;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(
        HttpClient httpClient,
        IAiPromptBuilder promptBuilder,
        IOptions<AiOptions> options,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _promptBuilder = promptBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiAskResponseDto> AskAsync(AiAskRequestDto request, AiContextDto context, CancellationToken cancellationToken = default)
    {
        if (request is null || context is null)
        {
            return new AiAskResponseDto
            {
                Success = false,
                Answer = string.Empty,
                Error = "Request payload is required."
            };
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.Model))
        {
            _logger.LogError("AI configuration is invalid. ApiKey or Model is missing.");
            return new AiAskResponseDto
            {
                Success = false,
                Answer = string.Empty,
                Error = SafeErrorMessage
            };
        }

        try
        {
            var configuredModel = _options.Model.Trim();
            var requestPath = $"models/{configuredModel}:generateContent";
            var requestUrl = new Uri(_httpClient.BaseAddress!, requestPath).ToString();
            var prompt = _promptBuilder.BuildFinancialAssistantPrompt(request, context);
            var geminiRequest = BuildGeminiRequest(prompt);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestPath)
            {
                Content = JsonContent.Create(geminiRequest, options: SerializerOptions)
            };

            httpRequest.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);

            _logger.LogInformation(
                "Sending Gemini request. Model={Model} Url={Url}",
                configuredModel,
                requestUrl);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Gemini request failed. Model={Model} Url={Url} StatusCode={StatusCode} ResponseBody={ResponseBody}",
                    configuredModel,
                    requestUrl,
                    (int)response.StatusCode,
                    responseBody);

                return new AiAskResponseDto
                {
                    Success = false,
                    Answer = string.Empty,
                    Error = SafeErrorMessage
                };
            }

            _logger.LogInformation(
                "Gemini request succeeded. Model={Model} Url={Url} StatusCode={StatusCode}",
                configuredModel,
                requestUrl,
                (int)response.StatusCode);

            GeminiGenerateContentResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseBody, SerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Gemini response deserialization failed. Model={Model} Url={Url} ResponseBody={ResponseBody}",
                    configuredModel,
                    requestUrl,
                    responseBody);

                return new AiAskResponseDto
                {
                    Success = false,
                    Answer = string.Empty,
                    Error = SafeErrorMessage
                };
            }

            var answer = ExtractAnswer(payload);

            if (string.IsNullOrWhiteSpace(answer))
            {
                _logger.LogWarning(
                    "Gemini returned an empty answer. Model={Model} Url={Url} ResponseBody={ResponseBody}",
                    configuredModel,
                    requestUrl,
                    responseBody);
                return new AiAskResponseDto
                {
                    Success = false,
                    Answer = string.Empty,
                    Error = SafeErrorMessage
                };
            }

            return new AiAskResponseDto
            {
                Success = true,
                Answer = answer
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Gemini AI request was canceled.");
            return new AiAskResponseDto
            {
                Success = false,
                Answer = string.Empty,
                Error = "AI request was canceled."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Gemini AI.");
            return new AiAskResponseDto
            {
                Success = false,
                Answer = string.Empty,
                Error = SafeErrorMessage
            };
        }
    }

    private GeminiGenerateContentRequest BuildGeminiRequest(string prompt)
    {
        return new GeminiGenerateContentRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Role = "user",
                    Parts =
                    [
                        new GeminiPart
                        {
                            Text = prompt
                        }
                    ]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = _options.Temperature,
                MaxOutputTokens = _options.MaxTokens
            }
        };
    }

    private static string ExtractAnswer(GeminiGenerateContentResponse? payload)
    {
        var parts = payload?.Candidates?
            .SelectMany(x => x.Content?.Parts ?? [])
            .Select(x => x.Text?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return parts is { Count: > 0 }
            ? string.Join(Environment.NewLine, parts)
            : string.Empty;
    }

    private sealed class GeminiGenerateContentRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }
    }

    private sealed class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
