using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementacao concreta da porta de roteamento de IA externa.
/// Encaminha consultas para provider real e aplica fallback explicito e controlado.
/// </summary>
public sealed class ExternalAiRoutingPortAdapter : IExternalAIRoutingPort
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly IAiModelCatalogService _modelCatalogService;
    private readonly AiRoutingOptions _options;
    private readonly ILogger<ExternalAiRoutingPortAdapter> _logger;

    public ExternalAiRoutingPortAdapter(
        IAiProviderFactory providerFactory,
        IAiModelCatalogService modelCatalogService,
        IOptions<AiRoutingOptions> options,
        ILogger<ExternalAiRoutingPortAdapter> logger)
    {
        _providerFactory = providerFactory;
        _modelCatalogService = modelCatalogService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> RouteQueryAsync(
        string context,
        string query,
        string? preferredProvider = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new InvalidOperationException("AI query must not be empty.");

        var resolvedModel = await _modelCatalogService.ResolveDefaultModelAsync("chat", cancellationToken);
        if (resolvedModel is null)
            throw new InvalidOperationException("No active chat model found in AI Model Registry.");

        var providerId = string.IsNullOrWhiteSpace(preferredProvider)
            ? (_options.PreferredProvider ?? resolvedModel.ProviderId)
            : preferredProvider;

        var chatProvider = _providerFactory.GetChatProvider(providerId);
        if (chatProvider is null && !providerId.Equals(resolvedModel.ProviderId, StringComparison.OrdinalIgnoreCase))
            chatProvider = _providerFactory.GetChatProvider(resolvedModel.ProviderId);

        if (chatProvider is null)
            throw new InvalidOperationException($"Chat provider '{providerId}' is not available.");

        var modelId = providerId.Equals(resolvedModel.ProviderId, StringComparison.OrdinalIgnoreCase)
            ? resolvedModel.ModelName
            : (_options.PreferredChatModel ?? resolvedModel.ModelName);

        var systemPrompt = BuildSystemPrompt(context);
        var messages = new List<ChatMessage>
        {
            new("system", systemPrompt),
            new("user", query)
        };

        try
        {
            var completion = await chatProvider.CompleteAsync(
                new ChatCompletionRequest(modelId, messages),
                cancellationToken);

            if (completion.Success && !string.IsNullOrWhiteSpace(completion.Content))
                return completion.Content;

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(completion.ErrorMessage)
                    ? "Provider returned empty response."
                    : completion.ErrorMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "AI routing failed for provider {ProviderId}. Fallback enabled: {FallbackEnabled}",
                providerId,
                _options.EnableDeterministicFallback);

            if (!_options.EnableDeterministicFallback)
                throw new InvalidOperationException(
                    $"AI provider '{providerId}' unavailable and deterministic fallback is disabled.", ex);

            return BuildExplicitFallbackResponse(providerId, context, query);
        }
    }

    private string BuildSystemPrompt(string context)
    {
        var scope = string.IsNullOrWhiteSpace(context)
            ? "No additional grounding context provided."
            : context;

        return "You are NexTraceOne AI Assistant. Use only the provided product context and grounding data. " +
               "If grounding is incomplete, state limitations explicitly. " +
               "Prioritize operational safety, contracts and change confidence.\n\n" +
               "Grounding context:\n" + scope;
    }

    private string BuildExplicitFallbackResponse(string providerId, string context, string query)
    {
        var contextSnippet = string.IsNullOrWhiteSpace(context)
            ? "No grounding context available."
            : context.Length > 900
                ? string.Concat(context.AsSpan(0, 900), "...")
                : context;

        var querySnippet = query.Length > 240
            ? string.Concat(query.AsSpan(0, 240), "...")
            : query;

        return $"{_options.FallbackPrefix} Provider '{providerId}' is unavailable. " +
               "This response is deterministic fallback and should be treated as limited guidance.\n\n" +
               $"Question: {querySnippet}\n" +
               $"Grounding snapshot: {contextSnippet}";
    }
}
