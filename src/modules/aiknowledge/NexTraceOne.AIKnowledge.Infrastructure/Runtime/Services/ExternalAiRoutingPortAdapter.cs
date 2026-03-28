using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementacao concreta da porta de roteamento de IA externa.
/// Encaminha consultas para provider real e aplica fallback explicito e controlado.
/// Antes de encaminhar, valida a capability contra políticas ativas de ExternalAI —
/// se uma política activa bloquear o contexto ou requerer aprovação, o pedido é rejeitado.
/// Em ambiente de produção, aplica regras adicionais de contenção de dados.
/// </summary>
public sealed class ExternalAiRoutingPortAdapter : IExternalAIRoutingPort
{
    private static readonly HashSet<string> ProductionEnvironmentNames =
        new(StringComparer.OrdinalIgnoreCase) { "production", "prod", "prd" };

    private readonly IAiProviderFactory _providerFactory;
    private readonly IAiModelCatalogService _modelCatalogService;
    private readonly IExternalAiPolicyRepository _policyRepository;
    private readonly AiRoutingOptions _options;
    private readonly ILogger<ExternalAiRoutingPortAdapter> _logger;

    public ExternalAiRoutingPortAdapter(
        IAiProviderFactory providerFactory,
        IAiModelCatalogService modelCatalogService,
        IExternalAiPolicyRepository policyRepository,
        IOptions<AiRoutingOptions> options,
        ILogger<ExternalAiRoutingPortAdapter> logger)
    {
        _providerFactory = providerFactory;
        _modelCatalogService = modelCatalogService;
        _policyRepository = policyRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> RouteQueryAsync(
        string context,
        string query,
        string? preferredProvider = null,
        string? capability = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new InvalidOperationException("AI query must not be empty.");

        // ── Data protection: validate capability against active ExternalAI policies ──
        if (!string.IsNullOrWhiteSpace(capability))
        {
            var policyViolation = await CheckDataProtectionPoliciesAsync(
                capability, environment, cancellationToken);

            if (policyViolation is not null)
            {
                _logger.LogWarning(
                    "ExternalAI routing blocked by policy for capability '{Capability}' in environment '{Environment}': {Reason}",
                    capability, environment ?? "unspecified", policyViolation);

                if (_options.EnableDeterministicFallback)
                    return BuildPolicyBlockedResponse(capability, policyViolation);

                throw new InvalidOperationException(
                    $"ExternalAI routing blocked by active policy: {policyViolation}");
            }
        }

        // Try resolving model from registry; fall back to routing options when registry is empty.
        var resolvedModel = await _modelCatalogService.ResolveDefaultModelAsync("chat", cancellationToken);

        var providerId = string.IsNullOrWhiteSpace(preferredProvider)
            ? (_options.PreferredProvider ?? resolvedModel?.ProviderId)
            : preferredProvider;

        // If no provider could be determined, there is nothing to route to.
        if (string.IsNullOrWhiteSpace(providerId))
        {
            if (_options.EnableDeterministicFallback)
                return BuildExplicitFallbackResponse("unknown", context, query);

            throw new InvalidOperationException(
                "No AI provider available: Model Registry is empty and no preferred provider is configured.");
        }

        var chatProvider = _providerFactory.GetChatProvider(providerId);

        // Secondary fallback: if the preferred/requested provider is not registered, try the resolved model's provider.
        if (chatProvider is null && resolvedModel is not null &&
            !providerId.Equals(resolvedModel.ProviderId, StringComparison.OrdinalIgnoreCase))
        {
            chatProvider = _providerFactory.GetChatProvider(resolvedModel.ProviderId);
            if (chatProvider is not null)
                providerId = resolvedModel.ProviderId;
        }

        // Tertiary fallback: try any registered provider.
        if (chatProvider is null)
        {
            var anyProvider = _providerFactory.GetAllProviders().OfType<IChatCompletionProvider>().FirstOrDefault();
            if (anyProvider is not null)
            {
                chatProvider = _providerFactory.GetChatProvider(anyProvider.ProviderId);
                if (chatProvider is not null)
                    providerId = anyProvider.ProviderId;
            }
        }

        if (chatProvider is null)
        {
            if (_options.EnableDeterministicFallback)
                return BuildExplicitFallbackResponse(providerId, context, query);

            throw new InvalidOperationException($"Chat provider '{providerId}' is not available.");
        }

        // Resolve model name: prefer configured override, then registry, then options default.
        var modelId = _options.PreferredChatModel
            ?? resolvedModel?.ModelName
            ?? providerId;

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

    /// <summary>
    /// Verifica políticas activas de ExternalAI contra a capability e o ambiente.
    /// Retorna a razão de bloqueio quando alguma política impede o envio, ou null quando permitido.
    /// Em produção, qualquer política activa que cubra a capability e requeira aprovação bloqueia.
    /// </summary>
    private async Task<string?> CheckDataProtectionPoliciesAsync(
        string capability,
        string? environment,
        CancellationToken ct)
    {
        var activePolicies = await _policyRepository.ListActiveAsync(ct);

        if (activePolicies.Count == 0)
            return null;

        var isProduction = !string.IsNullOrWhiteSpace(environment)
            && ProductionEnvironmentNames.Contains(environment);

        foreach (var policy in activePolicies)
        {
            // A policy that explicitly covers the capability and requires approval always blocks.
            if (policy.IsContextAllowed(capability) && policy.RequiresApproval)
            {
                return $"Policy '{policy.Name}' requires approval for capability '{capability}'.";
            }

            // In production environments, any active policy that covers the capability blocks
            // external routing to enforce data-leakage containment.
            if (isProduction && policy.IsContextAllowed(capability))
            {
                return $"Policy '{policy.Name}' blocks external AI routing for capability '{capability}' in production environment.";
            }
        }

        return null;
    }

    private string BuildPolicyBlockedResponse(string capability, string reason)
        => $"{_options.FallbackPrefix} ExternalAI routing blocked by governance policy for capability '{capability}'. " +
           $"Reason: {reason} " +
           "This response is a deterministic fallback. Review ExternalAI policies with your platform administrator.";
}
