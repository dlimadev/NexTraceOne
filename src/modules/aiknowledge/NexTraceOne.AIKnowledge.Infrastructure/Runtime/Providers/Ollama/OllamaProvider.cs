using System.Diagnostics;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

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
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(OllamaHttpClient client, ILogger<OllamaProvider> logger)
    {
        _client = client;
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
            var ollamaRequest = new OllamaChatRequest
            {
                Model = request.ModelId,
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
}
