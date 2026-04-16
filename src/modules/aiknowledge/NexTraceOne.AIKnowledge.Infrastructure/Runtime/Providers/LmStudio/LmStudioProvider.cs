using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.LmStudio;

/// <summary>
/// Provider de IA LM Studio — local, OpenAI-compatible, sem chave de API.
/// LM Studio é um servidor local que carrega modelos GGUF e expõe a API OpenAI
/// em http://localhost:1234/v1. Útil para ambientes on-prem sem acesso a Ollama.
/// Implementa IAiProvider e IChatCompletionProvider para uso pelo AI Runtime.
/// </summary>
public sealed class LmStudioProvider : IAiProvider, IChatCompletionProvider
{
    public const string ProviderIdentifier = "lmstudio";

    private static readonly string[] DefaultCapabilities = ["chat", "completion"];

    private readonly LmStudioHttpClient _client;
    private readonly LmStudioOptions _options;
    private readonly ILogger<LmStudioProvider> _logger;

    public LmStudioProvider(
        LmStudioHttpClient client,
        IOptions<LmStudioOptions> options,
        ILogger<LmStudioProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => ProviderIdentifier;
    public string DisplayName => "LM Studio (Local)";
    public bool IsLocal => true;

    /// <summary>Verifica se o servidor LM Studio está em execução e a responder.</summary>
    public async Task<AiProviderHealthResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var healthy = await _client.IsHealthyAsync(cancellationToken);
            sw.Stop();
            return new AiProviderHealthResult(
                healthy,
                ProviderId,
                healthy ? "LM Studio server is running" : "LM Studio server is not responding",
                sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "LM Studio health check failed");
            return new AiProviderHealthResult(
                false, ProviderId,
                $"Health check failed: {ex.Message}", sw.Elapsed);
        }
    }

    /// <summary>Lista todos os modelos actualmente disponíveis no LM Studio.</summary>
    public async Task<IReadOnlyList<AiProviderModelInfo>> ListAvailableModelsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.ListModelsAsync(cancellationToken);
            if (response?.Data is null)
                return [];

            return response.Data.Select(m => new AiProviderModelInfo(
                m.Id,
                m.Id,
                null,
                DefaultCapabilities
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list LM Studio models");
            return [];
        }
    }

    /// <summary>Executa uma inferência de chat/completion (one-shot) via LM Studio.</summary>
    public async Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var model = ResolveModelId(request.ModelId);
            var messages = BuildMessages(request);

            var chatRequest = new LmStudioChatRequest
            {
                Model = model,
                Messages = messages,
                Temperature = request.Temperature ?? _options.DefaultTemperature,
                MaxTokens = request.MaxTokens ?? _options.DefaultMaxTokens,
            };

            var response = await _client.ChatAsync(chatRequest, cancellationToken);
            sw.Stop();

            var content = response?.Choices.FirstOrDefault()?.Message?.Content;
            var usage = response?.Usage;

            return new ChatCompletionResult(
                Success: content is not null,
                Content: content,
                ModelId: model,
                ProviderId: ProviderId,
                PromptTokens: usage?.PromptTokens ?? 0,
                CompletionTokens: usage?.CompletionTokens ?? 0,
                Duration: sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "LM Studio completion failed for model {ModelId}", request.ModelId);
            return new ChatCompletionResult(
                Success: false,
                Content: null,
                ModelId: request.ModelId,
                ProviderId: ProviderId,
                PromptTokens: 0,
                CompletionTokens: 0,
                Duration: sw.Elapsed,
                ErrorMessage: ex.Message);
        }
    }

    /// <summary>
    /// Executa chat com streaming incremental.
    /// LM Studio suporta stream=true na API OpenAI, mas este provider retorna
    /// a resposta completa como chunk único para simplificar a integração inicial.
    /// </summary>
    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var result = await CompleteAsync(request, cancellationToken);

        yield return new ChatStreamChunk(
            Content: result.Content ?? string.Empty,
            IsComplete: true,
            ModelId: result.ModelId,
            ProviderId: ProviderId,
            PromptTokens: result.PromptTokens,
            CompletionTokens: result.CompletionTokens,
            ErrorMessage: result.ErrorMessage);
    }

    /// <summary>LM Studio suporta streaming nativo mas esta implementação usa modo single-chunk.</summary>
    public bool SupportsStreaming => false;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string ResolveModelId(string requestedModelId)
    {
        if (!string.IsNullOrWhiteSpace(requestedModelId)
            && requestedModelId != ProviderIdentifier)
        {
            return requestedModelId;
        }

        return !string.IsNullOrWhiteSpace(_options.DefaultChatModel)
            ? _options.DefaultChatModel
            : "loaded-model"; // LM Studio aceita "loaded-model" como alias para o modelo activo
    }

    private static List<LmStudioChatMessage> BuildMessages(ChatCompletionRequest request)
    {
        var messages = new List<LmStudioChatMessage>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new LmStudioChatMessage
            {
                Role = "system",
                Content = request.SystemPrompt
            });
        }

        messages.AddRange(request.Messages.Select(m => new LmStudioChatMessage
        {
            Role = m.Role,
            Content = m.Content
        }));

        return messages;
    }
}
