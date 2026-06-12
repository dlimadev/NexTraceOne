using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Gemini;

/// <summary>
/// Provider de IA Google Gemini — externo, configurável, sem SDK proprietário.
/// Implementa IAiProvider e IChatCompletionProvider para integração no AI Runtime.
/// Activo apenas quando ApiKey está configurada em AiRuntime:Gemini.
/// </summary>
public sealed class GeminiProvider : IAiProvider, IChatCompletionProvider
{
    public const string ProviderIdentifier = "gemini";

    private static readonly string[] DefaultCapabilities = ["chat", "completion", "reasoning"];

    private readonly GeminiHttpClient _client;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiProvider> _logger;

    public GeminiProvider(
        GeminiHttpClient client,
        IOptions<GeminiOptions> options,
        ILogger<GeminiProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;
    public string DisplayName => "Google Gemini";
    public bool IsLocal => false;

    public async Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var healthy = await _client.IsHealthyAsync(cancellationToken);
            sw.Stop();
            return new AiProviderHealthResult(
                healthy,
                ProviderId,
                healthy ? "Gemini API is reachable" : "Gemini API is not responding",
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Gemini health check failed");
            return new AiProviderHealthResult(
                false, ProviderId,
                $"Health check failed: {ex.Message}", sw.Elapsed);
        }
    }

    public Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AiProviderModelInfo> models =
        [
            new AiProviderModelInfo("gemini-1.5-pro", "Gemini 1.5 Pro", null, ["chat", "vision", "reasoning"]),
            new AiProviderModelInfo("gemini-1.5-flash", "Gemini 1.5 Flash", null, DefaultCapabilities),
        ];
        return Task.FromResult(models);
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrWhiteSpace(request.ModelId)
            ? _options.DefaultChatModel
            : request.ModelId;

        var sw = Stopwatch.StartNew();
        try
        {
            var contents = request.Messages.Select(m => new GeminiContent
            {
                Role = m.Role == "assistant" ? "model" : m.Role,
                Parts = [new GeminiPart { Text = m.Content }]
            }).ToList();

            var geminiRequest = new GeminiChatRequest
            {
                Model = modelId,
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = request.Temperature ?? _options.DefaultTemperature,
                    MaxOutputTokens = request.MaxTokens ?? _options.DefaultMaxTokens
                }
            };

            // Add system prompt as first user message if present (Gemini doesn't have native system role)
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                geminiRequest.Contents.Insert(0, new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = $"[System Instruction]: {request.SystemPrompt}" }]
                });
            }

            var response = await _client.GenerateContentAsync(geminiRequest, cancellationToken);
            sw.Stop();

            if (response?.Candidates is not { Count: > 0 } ||
                string.IsNullOrWhiteSpace(response.Candidates[0].Content?.Parts?[0].Text))
            {
                return new ChatCompletionResult(
                    false, null, modelId, ProviderId,
                    0, 0, sw.Elapsed, "No response from Gemini");
            }

            var candidate = response.Candidates[0];
            var content = candidate.Content?.Parts?[0].Text ?? string.Empty;

            return new ChatCompletionResult(
                true,
                content,
                modelId,
                ProviderId,
                response.UsageMetadata?.PromptTokenCount ?? 0,
                response.UsageMetadata?.CandidatesTokenCount ?? 0,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Gemini chat completion failed for model {Model}", modelId);
            return new ChatCompletionResult(
                false, null, modelId, ProviderId,
                0, 0, sw.Elapsed, ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var modelId = string.IsNullOrWhiteSpace(request.ModelId)
            ? _options.DefaultChatModel
            : request.ModelId;

        var contents = request.Messages.Select(m => new GeminiContent
        {
            Role = m.Role == "assistant" ? "model" : m.Role,
            Parts = [new GeminiPart { Text = m.Content }]
        }).ToList();

        var geminiRequest = new GeminiChatRequest
        {
            Model = modelId,
            Contents = contents,
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = request.Temperature ?? _options.DefaultTemperature,
                MaxOutputTokens = request.MaxTokens ?? _options.DefaultMaxTokens
            }
        };

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            geminiRequest.Contents.Insert(0, new GeminiContent
            {
                Role = "user",
                Parts = [new GeminiPart { Text = $"[System Instruction]: {request.SystemPrompt}" }]
            });
        }

        var hasYielded = false;
        await foreach (var chunk in _client.StreamGenerateContentAsync(geminiRequest, cancellationToken))
        {
            hasYielded = true;
            var content = string.Empty;
            var finishReason = (string?)null;

            if (chunk.Candidates is { Count: > 0 })
            {
                content = chunk.Candidates[0].Content?.Parts?[0].Text ?? string.Empty;
                finishReason = chunk.Candidates[0].FinishReason;
            }

            var isComplete = finishReason is "STOP" or "MAX_TOKENS";

            yield return new ChatStreamChunk(
                content,
                isComplete,
                modelId,
                ProviderId,
                isComplete ? (chunk.UsageMetadata?.PromptTokenCount ?? 0) : 0,
                isComplete ? (chunk.UsageMetadata?.CandidatesTokenCount ?? 0) : 0);

            if (isComplete)
                yield break;
        }

        if (!hasYielded)
        {
            yield return new ChatStreamChunk(
                string.Empty, true, modelId, ProviderId,
                ErrorMessage: "No streaming response from Gemini");
        }
    }
}
