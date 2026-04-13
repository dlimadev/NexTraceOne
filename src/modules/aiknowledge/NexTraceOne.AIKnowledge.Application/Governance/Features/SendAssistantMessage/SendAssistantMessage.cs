using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;

/// <summary>
/// Feature: SendAssistantMessage — envia uma mensagem ao assistente de IA governado.
/// Valida políticas de acesso, regista auditoria de uso, persiste mensagens na conversa
/// e retorna resposta com metadados completos de grounding, roteamento e explicabilidade.
/// Integra pipeline de roteamento inteligente e enriquecimento de contexto.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SendAssistantMessage
{
    private const string DegradedProviderId = "system-fallback";
    private const string DegradedModelName = "deterministic-fallback";

    private static string ResolveConversationOwner(ICurrentUser currentUser)
        => !string.IsNullOrWhiteSpace(currentUser.Email)
            ? currentUser.Email
            : currentUser.Id;

    private static bool IsConversationOwner(AiAssistantConversation conversation, ICurrentUser currentUser)
        => string.Equals(conversation.CreatedBy, currentUser.Id, StringComparison.OrdinalIgnoreCase)
           || string.Equals(conversation.CreatedBy, currentUser.Email, StringComparison.OrdinalIgnoreCase);

    /// <summary>Comando de envio de mensagem ao assistente de IA com contexto completo.</summary>
    public sealed record Command(
        Guid? ConversationId,
        string Message,
        string? ContextScope,
        string? Persona,
        Guid? PreferredModelId,
        string ClientType,
        Guid? ServiceId,
        Guid? ContractId,
        Guid? IncidentId,
        Guid? ChangeId,
        Guid? TeamId,
        Guid? DomainId,
        string? ContextBundle = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de envio de mensagem.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Message).NotEmpty().MaximumLength(10_000);
            RuleFor(x => x.ClientType).NotEmpty();
        }
    }

    /// <summary>Handler que processa a mensagem do assistente com roteamento e governança.</summary>
    public sealed class Handler(
        IAiUsageEntryRepository usageEntryRepository,
        IAiRoutingStrategyRepository routingStrategyRepository,
        IAiKnowledgeSourceRepository knowledgeSourceRepository,
        IAiModelAuthorizationService modelAuthorizationService,
        IAiTokenQuotaService tokenQuotaService,
        IExternalAIRoutingPort externalAiRoutingPort,
        IAiProviderFactory providerFactory,
        IContextGroundingService contextGroundingService,
        IAiRoutingResolver routingResolver,
        IConversationPersistenceService conversationPersistenceService,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        ICurrentEnvironment currentEnvironment,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        private const string DegradedProviderId = "system-fallback";
        private const string DegradedModelName = "deterministic-fallback";

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var correlationId = Guid.NewGuid().ToString();
            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;
            var persona = request.Persona ?? "Engineer";

            // 1. Resolve or create conversation
            var (conversation, convError) = await conversationPersistenceService.GetOrCreateAsync(
                request.ConversationId,
                currentUser.Id,
                currentUser.Email,
                request.Message,
                persona,
                clientType,
                request.ContextScope,
                request.ServiceId,
                request.ContractId,
                request.IncidentId,
                request.ChangeId,
                request.TeamId,
                cancellationToken);

            if (convError is not null)
                return convError;

            if (conversation is null)
                return AiGovernanceErrors.ConversationNotFound("unknown");

            // 2. Resolve knowledge sources and grounding context
            var sources = await knowledgeSourceRepository.ListAsync(sourceType: null, isActive: true, cancellationToken);
            var groundingResult = await contextGroundingService.ResolveGroundingAsync(
                request.Message,
                persona,
                request.ContextScope,
                request.ServiceId,
                request.ContractId,
                request.IncidentId,
                request.ChangeId,
                request.TeamId,
                request.DomainId,
                request.ContextBundle,
                sources,
                cancellationToken);

            // 3. Resolve routing (model + provider)
            var activeStrategies = await routingStrategyRepository.ListAsync(isActive: true, cancellationToken);
            var routingResult = await routingResolver.ResolveRoutingAsync(
                persona,
                groundingResult.UseCaseType,
                request.ClientType,
                request.PreferredModelId,
                groundingResult.ConfidenceLevel,
                activeStrategies,
                cancellationToken);

            // 4. Validate model authorization
            if (request.PreferredModelId.HasValue)
            {
                var accessDecision = await modelAuthorizationService.ValidateModelAccessAsync(
                    request.PreferredModelId.Value, cancellationToken);

                if (!accessDecision.IsAllowed)
                {
                    logger.LogWarning(
                        "User {UserId} denied access to model {ModelId}: {Reason}",
                        currentUser.Id, request.PreferredModelId.Value, accessDecision.DenialReason);

                    return AiGovernanceErrors.ModelAccessDenied(
                        request.PreferredModelId.Value.ToString(),
                        accessDecision.DenialReason ?? "Access denied by policy");
                }
            }

            // 5. Pre-validate token quota
            var estimatedTokens = ContextWindowManager.EstimateTokens(request.Message) +
                                  ContextWindowManager.EstimateTokens(groundingResult.GroundingContext);

            var quotaResult = await tokenQuotaService.ValidateQuotaAsync(
                currentUser.Id,
                currentTenant.Id,
                routingResult.SelectedProvider,
                routingResult.SelectedModel,
                estimatedTokens,
                cancellationToken);

            if (!quotaResult.IsAllowed)
            {
                logger.LogWarning(
                    "Token quota exceeded for user {UserId}, tenant {TenantId}: {BlockReason}",
                    currentUser.Id, currentTenant.Id, quotaResult.BlockReason);

                return AiGovernanceErrors.QuotaExceeded("user", currentUser.Id);
            }

            // 6. Execute inference
            var selectedModel = routingResult.SelectedModel;
            var selectedProvider = routingResult.SelectedProvider;
            var isInternal = routingResult.IsInternal;
            string grounded;
            string? degradedReason = null;
            int promptTokens;
            int completionTokens;

            try
            {
                var chatProvider = providerFactory.GetChatProvider(selectedProvider);
                if (chatProvider is not null)
                {
                    logger.LogDebug(
                        "Calling IChatCompletionProvider {ProviderId} model {ModelId} for conversation {ConversationId}",
                        chatProvider.ProviderId, selectedModel, conversation.Id.Value);

                    var messages = new List<ChatMessage> { new("user", request.Message) };
                    var chatRequest = new ChatCompletionRequest(selectedModel, messages, SystemPrompt: groundingResult.SystemPrompt);
                    var result = await chatProvider.CompleteAsync(chatRequest, cancellationToken);

                    if (result.Success && !string.IsNullOrWhiteSpace(result.Content))
                    {
                        grounded = result.Content;
                        promptTokens = result.PromptTokens > 0
                            ? result.PromptTokens
                            : Math.Max(1, (request.Message.Length + groundingResult.GroundingContext.Length) / 4);
                        completionTokens = result.CompletionTokens > 0
                            ? result.CompletionTokens
                            : Math.Max(1, grounded.Length / 4);
                        if (!string.IsNullOrWhiteSpace(result.ModelId)) selectedModel = result.ModelId;
                        if (!string.IsNullOrWhiteSpace(result.ProviderId)) selectedProvider = result.ProviderId;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.IsNullOrWhiteSpace(result.ErrorMessage)
                                ? "Provider returned empty response."
                                : result.ErrorMessage);
                    }
                }
                else
                {
                    logger.LogDebug(
                        "No local provider registered for {ProviderId}; routing via IExternalAIRoutingPort",
                        selectedProvider);

                    var routingEnvironment = currentEnvironment.IsResolved && currentEnvironment.IsProductionLike
                        ? "production"
                        : null;

                    grounded = await externalAiRoutingPort.RouteQueryAsync(
                        groundingResult.GroundingContext,
                        request.Message,
                        preferredProvider: selectedProvider,
                        capability: groundingResult.UseCaseType.ToString(),
                        environment: routingEnvironment,
                        cancellationToken: cancellationToken);

                    promptTokens = Math.Max(1, (request.Message.Length + groundingResult.GroundingContext.Length) / 4);
                    completionTokens = Math.Max(1, grounded.Length / 4);
                    if (string.IsNullOrWhiteSpace(selectedModel)) selectedModel = "external-routed";
                    if (string.IsNullOrWhiteSpace(selectedProvider)) selectedProvider = "external-routing-port";
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex,
                    "AI provider routing failed for conversation {ConversationId}. Persisting explicit degraded response.",
                    conversation.Id.Value);

                degradedReason = AiMessage.ProviderUnavailableReason;
                grounded = BuildExplicitProviderUnavailableResponse(request.Message, groundingResult.GroundingContext, ex.Message);
                selectedModel = DegradedModelName;
                selectedProvider = DegradedProviderId;
                isInternal = true;
                promptTokens = Math.Max(1, (request.Message.Length + groundingResult.GroundingContext.Length) / 4);
                completionTokens = Math.Max(1, grounded.Length / 4);
            }

            var usedDeterministicFallback = grounded.StartsWith(AiMessage.DeterministicFallbackPrefix, StringComparison.OrdinalIgnoreCase);
            var contextCaveats = groundingResult.Caveats?.ToList() ?? [];

            if (usedDeterministicFallback)
            {
                degradedReason ??= AiMessage.ProviderUnavailableReason;
                contextCaveats.Add("Provider unavailable at request time. Explicit degraded response was persistida.");
            }

            // Merge bundle relations into context refs
            var contextRefs = ResolveContextReferences(request);
            if (!string.IsNullOrWhiteSpace(request.ContextBundle))
            {
                try
                {
                    var bundle = System.Text.Json.JsonSerializer.Deserialize<ContextBundleData>(
                        request.ContextBundle, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (bundle?.Relations is { Count: > 0 })
                        foreach (var rel in bundle.Relations)
                        {
                            var refStr = $"{rel.EntityType}:{rel.Name}";
                            if (!contextRefs.Contains(refStr)) contextRefs.Add(refStr);
                        }
                }
                catch (System.Text.Json.JsonException) { /* ignore */ }
            }

            // 7. Persist message pair
            var persistResult = await conversationPersistenceService.PersistMessagePairAsync(
                conversation,
                request.Message,
                DateTimeOffset.UtcNow,
                grounded,
                selectedModel,
                selectedProvider,
                isInternal,
                promptTokens,
                completionTokens,
                routingResult.AppliedStrategy?.Name,
                string.Join(",", groundingResult.GroundingSources),
                string.Join(",", contextRefs),
                correlationId,
                isDegraded: usedDeterministicFallback,
                cancellationToken);

            // 8. Record usage audit entry
            var usageEntry = AIUsageEntry.Record(
                currentUser.Id,
                currentUser.Name,
                request.PreferredModelId ?? Guid.Empty,
                selectedModel,
                selectedProvider,
                isInternal: isInternal,
                persistResult.UserMessageTimestamp,
                promptTokens,
                completionTokens,
                policyId: null,
                policyName: routingResult.AppliedStrategy?.Name,
                UsageResult.Allowed,
                request.ContextScope ?? string.Empty,
                clientType,
                correlationId,
                conversation.Id.Value);

            await usageEntryRepository.AddAsync(usageEntry, cancellationToken);

            var routingPath = usedDeterministicFallback ? "ProviderUnavailableFallback" : routingResult.RoutingPath.ToString();
            var confidenceLevel = usedDeterministicFallback ? AIConfidenceLevel.Low.ToString()
                : !string.IsNullOrWhiteSpace(request.ContextBundle) ? "High"
                : groundingResult.ConfidenceLevel;

            return new Response(
                persistResult.ConversationId,
                persistResult.AssistantMessageId,
                grounded.StartsWith(AiMessage.DeterministicFallbackPrefix, StringComparison.OrdinalIgnoreCase)
                    ? grounded
                    : grounded,
                selectedModel,
                selectedProvider,
                IsInternalModel: isInternal,
                PromptTokens: promptTokens,
                CompletionTokens: completionTokens,
                AppliedPolicy: routingResult.AppliedStrategy?.Name,
                GroundingSources: groundingResult.GroundingSources.ToList(),
                ContextReferences: contextRefs,
                CorrelationId: correlationId,
                UseCaseType: groundingResult.UseCaseType.ToString(),
                RoutingPath: routingPath,
                ConfidenceLevel: confidenceLevel,
                CostClass: usedDeterministicFallback ? "none" : routingResult.CostClass,
                RoutingRationale: usedDeterministicFallback
                    ? $"Provider unavailable. Explicit degraded response persisted. Original rationale: {routingResult.RoutingRationale}"
                    : routingResult.RoutingRationale,
                SourceWeightingSummary: groundingResult.SourceWeightingSummary,
                EscalationReason: usedDeterministicFallback ? "ProviderUnavailableFallback" : routingResult.EscalationReason,
                ContextSummary: groundingResult.ContextSummary,
                SuggestedNextSteps: groundingResult.SuggestedSteps?.ToList(),
                ContextCaveats: contextCaveats.Count > 0 ? contextCaveats : null,
                ContextStrength: usedDeterministicFallback ? "partial" : groundingResult.ContextStrength,
                UserMessageId: persistResult.UserMessageId,
                UserMessageTimestamp: persistResult.UserMessageTimestamp,
                AssistantMessageTimestamp: persistResult.AssistantMessageTimestamp,
                ResponseState: persistResult.ResponseState,
                IsDegraded: persistResult.IsDegraded,
                DegradedReason: degradedReason ?? persistResult.DegradedReason,
                ConversationTitle: persistResult.ConversationTitle,
                ConversationMessageCount: persistResult.ConversationMessageCount,
                ConversationLastMessageAt: persistResult.ConversationLastMessageAt);
        }

        private static string BuildExplicitProviderUnavailableResponse(string query, string groundingContext, string errorMessage)
        {
            var contextSnippet = string.IsNullOrWhiteSpace(groundingContext)
                ? "No grounding context available."
                : groundingContext.Length > 900
                    ? string.Concat(groundingContext.AsSpan(0, 900), "...")
                    : groundingContext;

            var querySnippet = query.Length > 240
                ? string.Concat(query.AsSpan(0, 240), "...")
                : query;

            return $"{AiMessage.DeterministicFallbackPrefix} Provider unavailable. This response is degraded and should be treated as limited guidance. Operational detail: {errorMessage}.\n\nQuestion: {querySnippet}\nGrounding snapshot: {contextSnippet}";
        }

        private static List<string> ResolveContextReferences(Command request)
        {
            var refs = new List<string>();
            if (request.ServiceId.HasValue) refs.Add($"service:{request.ServiceId.Value}");
            if (request.ContractId.HasValue) refs.Add($"contract:{request.ContractId.Value}");
            if (request.IncidentId.HasValue) refs.Add($"incident:{request.IncidentId.Value}");
            if (request.ChangeId.HasValue) refs.Add($"change:{request.ChangeId.Value}");
            if (request.TeamId.HasValue) refs.Add($"team:{request.TeamId.Value}");
            if (request.DomainId.HasValue) refs.Add($"domain:{request.DomainId.Value}");

            if (!string.IsNullOrWhiteSpace(request.ContextScope))
            {
                var scopes = request.ContextScope.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var scope in scopes)
                {
                    var scopeRef = $"scope:{scope.ToLowerInvariant()}";
                    if (!refs.Contains(scopeRef)) refs.Add(scopeRef);
                }
            }

            return refs;
        }
    }

    /// <summary>Resposta madura do envio de mensagem com metadados de roteamento e enriquecimento.</summary>
    public sealed record Response(
        Guid ConversationId,
        Guid MessageId,
        string AssistantResponse,
        string ModelUsed,
        string Provider,
        bool IsInternalModel,
        int PromptTokens,
        int CompletionTokens,
        string? AppliedPolicy,
        List<string> GroundingSources,
        List<string> ContextReferences,
        string CorrelationId,
        string UseCaseType,
        string RoutingPath,
        string ConfidenceLevel,
        string CostClass,
        string RoutingRationale,
        string SourceWeightingSummary,
        string EscalationReason,
        string? ContextSummary = null,
        List<string>? SuggestedNextSteps = null,
        List<string>? ContextCaveats = null,
        string? ContextStrength = null,
        Guid? UserMessageId = null,
        DateTimeOffset? UserMessageTimestamp = null,
        DateTimeOffset? AssistantMessageTimestamp = null,
        string? ResponseState = null,
        bool IsDegraded = false,
        string? DegradedReason = null,
        string? ConversationTitle = null,
        int? ConversationMessageCount = null,
        DateTimeOffset? ConversationLastMessageAt = null);
}
/// <summary>
/// Bundle de contexto rico enviado pelo frontend com dados reais da entidade em análise.
/// Estrutura JSON com propriedades específicas por tipo de contexto (service, contract, change, incident).
/// </summary>
public sealed record ContextBundleData(
    string EntityType,
    string EntityName,
    string? EntityStatus,
    string? EntityDescription,
    Dictionary<string, string>? Properties,
    List<ContextBundleRelation>? Relations,
    List<string>? Caveats);

/// <summary>Relação com entidade associada dentro do context bundle.</summary>
public sealed record ContextBundleRelation(
    string RelationType,
    string EntityType,
    string Name,
    string? Status,
    Dictionary<string, string>? Properties);
