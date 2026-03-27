using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Ollama;

/// <summary>
/// Provider de IA Ollama — local, configurável, desacoplado.
/// Implementa IAiProvider e IChatCompletionProvider para uso pelo AI Runtime.
/// </summary>
public sealed class OllamaProvider : IAiProvider, IChatCompletionProvider
{
    public const string ProviderIdentifier = "ollama";

    private static readonly string[] DefaultCapabilities = ["chat", "completion"];

    private readonly OllamaHttpClient _client;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(
        OllamaHttpClient client,
        IOptions<OllamaOptions> options,
        ILogger<OllamaProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;
    public string DisplayName => "Ollama (Local)";
    public bool IsLocal => true;

    public async Task<AiProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var healthy = await _client.IsHealthyAsync(cancellationToken);
            sw.Stop();
            return new AiProviderHealthResult(healthy, ProviderId,
                healthy ? "Ollama is running" : "Ollama is not responding",
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Ollama health check failed");
            return new AiProviderHealthResult(false, ProviderId,
                $"Health check failed: {ex.Message}", sw.Elapsed);
        }
    }

    public async Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _client.ListModelsAsync(cancellationToken);
            if (tags?.Models is null)
                return [];

            return tags.Models.Select(m => new AiProviderModelInfo(
                m.Name,
                m.Name,
                m.Size,
                DefaultCapabilities
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list Ollama models");
            return [];
        }
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var selectedModel = await ResolveModelAsync(request.ModelId, cancellationToken);

            var ollamaRequest = new OllamaChatRequest
            {
                Model = selectedModel,
                Stream = false,
                Messages = request.Messages.Select(m => new OllamaChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Options = new OllamaChatOptions
                {
                    Temperature = request.Temperature,
                    NumPredict = request.MaxTokens
                }
            };

            var response = await _client.ChatAsync(ollamaRequest, cancellationToken);
            sw.Stop();

            if (response?.Message is null)
            {
                return new ChatCompletionResult(
                    false, null, request.ModelId, ProviderId,
                    0, 0, sw.Elapsed, "No response from Ollama");
            }

            return new ChatCompletionResult(
                true,
                response.Message.Content,
                response.Model,
                ProviderId,
                response.PromptEvalCount,
                response.EvalCount,
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Ollama chat completion failed for model {Model}", request.ModelId);
            return new ChatCompletionResult(
                false, null, request.ModelId, ProviderId,
                0, 0, sw.Elapsed, ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? selectedModel = null;
        string? resolveError = null;
        try
        {
            selectedModel = await ResolveModelAsync(request.ModelId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama model resolution failed for streaming request");
            resolveError = $"Model resolution failed: {ex.Message}";
        }

        if (resolveError is not null)
        {
            yield return new ChatStreamChunk(
                string.Empty, true, request.ModelId, ProviderId,
                ErrorMessage: resolveError);
            yield break;
        }

        var ollamaRequest = new OllamaChatRequest
        {
            Model = selectedModel!,
            Stream = true,
            Messages = request.Messages.Select(m => new OllamaChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            Options = new OllamaChatOptions
            {
                Temperature = request.Temperature,
                NumPredict = request.MaxTokens
            }
        };

        var hasYielded = false;

        await foreach (var chunk in _client.ChatStreamAsync(ollamaRequest, cancellationToken))
        {
            hasYielded = true;
            var content = chunk.Message?.Content ?? string.Empty;

            yield return new ChatStreamChunk(
                content,
                chunk.Done,
                chunk.Model,
                ProviderId,
                chunk.Done ? chunk.PromptEvalCount : 0,
                chunk.Done ? chunk.EvalCount : 0);

            if (chunk.Done)
                yield break;
        }

        if (!hasYielded)
        {
            yield return new ChatStreamChunk(
                string.Empty, true, selectedModel!, ProviderId,
                ErrorMessage: "No streaming response from Ollama");
        }
    }

    private async Task<string> ResolveModelAsync(string requestedModel, CancellationToken cancellationToken)
    {
        var candidate = string.IsNullOrWhiteSpace(requestedModel)
            ? _options.DefaultChatModel
            : requestedModel;

        var tags = await _client.ListModelsAsync(cancellationToken);
        var availableModels = tags?.Models?.Select(m => m.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? [];

        if (availableModels.Count == 0)
            return candidate;

        if (availableModels.Any(m => string.Equals(m, candidate, StringComparison.OrdinalIgnoreCase)))
            return candidate;

        if (availableModels.Any(m => string.Equals(m, _options.DefaultChatModel, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning(
                "Requested Ollama model {RequestedModel} not available. Falling back to configured default model {DefaultModel}.",
                candidate,
                _options.DefaultChatModel);
            return _options.DefaultChatModel;
        }

        var requestedFamily = candidate.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(requestedFamily))
        {
            var familyMatch = availableModels.FirstOrDefault(
                m => m.StartsWith($"{requestedFamily}:", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(familyMatch))
            {
                _logger.LogWarning(
                    "Requested Ollama model {RequestedModel} not available. Falling back to same-family model {FallbackModel}.",
                    candidate,
                    familyMatch);
                return familyMatch;
            }
        }

        var firstAvailable = availableModels[0];
        _logger.LogWarning(
            "Requested Ollama model {RequestedModel} not available. Falling back to first available model {FallbackModel}.",
            candidate,
            firstAvailable);

        return firstAvailable;
    }
}
