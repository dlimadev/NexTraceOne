using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.OpenAI;

/// <summary>
/// Provider de IA OpenAI — externo, configurável, sem SDK proprietário.
/// Implementa IAiProvider e IChatCompletionProvider para integração no AI Runtime.
/// Activo apenas quando ApiKey está configurada em AiRuntime:OpenAI.
/// </summary>
public sealed class OpenAiProvider : IAiProvider, IChatCompletionProvider
{
    public const string ProviderIdentifier = "openai";

    private static readonly string[] DefaultCapabilities = ["chat", "completion", "reasoning"];

    private readonly OpenAiHttpClient _client;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiProvider> _logger;

    public OpenAiProvider(
        OpenAiHttpClient client,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;
    public string DisplayName => "OpenAI";
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
                healthy ? "OpenAI API is reachable" : "OpenAI API is not responding",
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "OpenAI health check failed");
            return new AiProviderHealthResult(
                false, ProviderId,
                $"Health check failed: {ex.Message}", sw.Elapsed);
        }
    }

    public Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        // Static list — OpenAI models do not change frequently enough to warrant a live query.
        IReadOnlyList<AiProviderModelInfo> models =
        [
            new AiProviderModelInfo("gpt-4o", "GPT-4o", null, ["chat", "vision", "reasoning"]),
            new AiProviderModelInfo("gpt-4o-mini", "GPT-4o Mini", null, DefaultCapabilities),
            new AiProviderModelInfo("gpt-4-turbo", "GPT-4 Turbo", null, DefaultCapabilities),
            new AiProviderModelInfo("gpt-3.5-turbo", "GPT-3.5 Turbo", null, DefaultCapabilities),
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
            var openAiMessages = request.Messages.Select(m => new OpenAiChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList();

            var openAiRequest = new OpenAiChatRequest
            {
                Model = modelId,
                Messages = openAiMessages,
                Temperature = request.Temperature ?? _options.DefaultTemperature,
                MaxTokens = request.MaxTokens ?? _options.DefaultMaxTokens,
            };

            var response = await _client.ChatAsync(openAiRequest, cancellationToken);
            sw.Stop();

            if (response?.Choices is not { Count: > 0 } ||
                string.IsNullOrWhiteSpace(response.Choices[0].Message?.Content))
            {
                return new ChatCompletionResult(
                    false, null, modelId, ProviderId,
                    0, 0, sw.Elapsed, "No response from OpenAI");
            }

            return new ChatCompletionResult(
                true,
                response.Choices[0].Message!.Content,
                response.Model,
                ProviderId,
                response.Usage?.PromptTokens ?? 0,
                response.Usage?.CompletionTokens ?? 0,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "OpenAI chat completion failed for model {Model}", modelId);
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

        var openAiMessages = request.Messages.Select(m => new OpenAiChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        var openAiRequest = new OpenAiChatRequest
        {
            Model = modelId,
            Messages = openAiMessages,
            Temperature = request.Temperature ?? _options.DefaultTemperature,
            MaxTokens = request.MaxTokens ?? _options.DefaultMaxTokens,
        };

        var hasYielded = false;
        string resolvedModel = modelId;

        await foreach (var chunk in _client.ChatStreamAsync(openAiRequest, cancellationToken))
        {
            hasYielded = true;
            if (!string.IsNullOrWhiteSpace(chunk.Model))
                resolvedModel = chunk.Model;

            var content = string.Empty;
            var finishReason = (string?)null;

            if (chunk.Choices is { Count: > 0 })
            {
                content = chunk.Choices[0].Delta?.Content ?? string.Empty;
                finishReason = chunk.Choices[0].FinishReason;
            }

            var isComplete = finishReason is "stop" or "length";

            yield return new ChatStreamChunk(
                content,
                isComplete,
                resolvedModel,
                ProviderId,
                isComplete ? (chunk.Usage?.PromptTokens ?? 0) : 0,
                isComplete ? (chunk.Usage?.CompletionTokens ?? 0) : 0);

            if (isComplete)
                yield break;
        }

        if (!hasYielded)
        {
            yield return new ChatStreamChunk(
                string.Empty, true, modelId, ProviderId,
                ErrorMessage: "No streaming response from OpenAI");
        }
    }
}
