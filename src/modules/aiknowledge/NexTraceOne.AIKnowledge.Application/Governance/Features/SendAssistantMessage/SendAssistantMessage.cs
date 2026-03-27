using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
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

    /// <summary>
    /// Bundle de contexto rico enviado pelo frontend com dados reais da entidade em análise.
    /// Estrutura JSON com propriedades específicas por tipo de contexto (service, contract, change, incident).
    /// Permite ao handler produzir respostas grounded sem necessidade de cross-module queries.
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
        IAiAssistantConversationRepository conversationRepository,
        IAiMessageRepository messageRepository,
        IAiRoutingStrategyRepository routingStrategyRepository,
        IAiKnowledgeSourceRepository knowledgeSourceRepository,
        IAiModelCatalogService modelCatalogService,
        IAiModelAuthorizationService modelAuthorizationService,
        IExternalAIRoutingPort externalAiRoutingPort,
        IDocumentRetrievalService documentRetrievalService,
        IDatabaseRetrievalService databaseRetrievalService,
        ITelemetryRetrievalService telemetryRetrievalService,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var correlationId = Guid.NewGuid().ToString();
            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;
            var persona = request.Persona ?? "Engineer";

            AiAssistantConversation? conversation = null;
            if (request.ConversationId.HasValue)
            {
                var convId = AiAssistantConversationId.From(request.ConversationId.Value);
                conversation = await conversationRepository.GetByIdAsync(convId, cancellationToken);

                if (conversation is null)
                    return AiGovernanceErrors.ConversationNotFound(request.ConversationId.Value.ToString());

                if (!IsConversationOwner(conversation, currentUser))
                    return AiGovernanceErrors.ConversationAccessDenied(request.ConversationId.Value.ToString());
            }

            if (conversation is null)
            {
                conversation = AiAssistantConversation.Start(
                    request.Message.Length > 100
                        ? string.Concat(request.Message.AsSpan(0, 97), "...")
                        : request.Message,
                    persona,
                    clientType,
                    request.ContextScope ?? string.Empty,
                    ResolveConversationOwner(currentUser),
                    serviceId: request.ServiceId,
                    contractId: request.ContractId,
                    incidentId: request.IncidentId,
                    teamId: request.TeamId,
                    changeId: request.ChangeId);
                await conversationRepository.AddAsync(conversation, cancellationToken);
            }

            if (!conversation.IsActive)
                return AiGovernanceErrors.ConversationNotActive(conversation.Id.Value.ToString());

            var conversationId = conversation.Id.Value;

            // ── Persistir mensagem do utilizador ──────────────────────────
            var userMessage = AiMessage.UserMessage(conversationId, request.Message, now);
            conversation.RecordMessage(null, now);
            await messageRepository.AddAsync(userMessage, cancellationToken);

            // ── Classificar caso de uso ──────────────────────────────────
            var useCaseType = ClassifyUseCase(request.Message, request.ContextScope);

            // ── Aplicar roteamento inteligente ───────────────────────────
            var strategies = await routingStrategyRepository.ListAsync(isActive: true, cancellationToken);
            var applicableStrategy = strategies
                .Where(s => s.IsApplicable(persona, useCaseType.ToString(), request.ClientType))
                .OrderBy(s => s.Priority)
                .FirstOrDefault();

            var routingPath = applicableStrategy?.PreferredPath ?? AIRoutingPath.InternalOnly;
            var (selectedModel, selectedProvider, isInternal) = SelectModel(useCaseType, persona, routingPath);

            var resolvedModel = request.PreferredModelId.HasValue
                ? await modelCatalogService.ResolveModelByIdAsync(request.PreferredModelId.Value, cancellationToken)
                : await modelCatalogService.ResolveDefaultModelAsync("chat", cancellationToken);

            if (resolvedModel is not null)
            {
                selectedModel = resolvedModel.ModelName;
                selectedProvider = resolvedModel.ProviderId;
                isInternal = resolvedModel.IsInternal;
            }

            // ── Validar autorização do modelo selecionado ────────────────
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

            // ── Resolver fontes e pesos de contexto ──────────────────────
            var sources = await knowledgeSourceRepository.ListAsync(
                sourceType: null, isActive: true, cancellationToken);
            var groundingSources = ResolveGroundingSources(request.ContextScope, sources, useCaseType);
            var contextRefs = ResolveContextReferences(request);
            var (sourceWeightingSummary, confidenceLevel) = EvaluateSourceWeights(sources, useCaseType);

            // ── Construir justificativa de roteamento ────────────────────
            var routingRationale = BuildRoutingRationale(
                persona, useCaseType, routingPath, selectedModel,
                isInternal, applicableStrategy?.Name, confidenceLevel);

            var costClass = isInternal
                ? (useCaseType is AIUseCaseType.ContractGeneration or AIUseCaseType.ChangeAnalysis ? "medium" : "low")
                : "high";

            var escalationReason = isInternal
                ? AIEscalationReason.None.ToString()
                : AIEscalationReason.ComplexityRequiresAdvancedModel.ToString();

            // ── Desserializar context bundle (se disponível) ──────────────
            ContextBundleData? contextBundle = null;
            var bundleParseError = false;
            if (!string.IsNullOrWhiteSpace(request.ContextBundle))
            {
                try
                {
                    contextBundle = JsonSerializer.Deserialize<ContextBundleData>(
                        request.ContextBundle, JsonOptions);
                }
                catch (JsonException)
                {
                    // Invalid JSON — proceed without bundle, flag for caveat
                    bundleParseError = true;
                }
            }

            // ── Preparar grounding e executar inferência real ────────────
            var (_, contextSummary, suggestedSteps, caveats, contextStrength) =
                contextBundle is not null
                    ? GenerateGroundedResponse(request.Message, persona, useCaseType, groundingSources, contextBundle)
                    : (string.Empty, (string?)null, (List<string>?)null, (List<string>?)null, "none");

            var groundingContext = BuildGroundingContext(
                request,
                persona,
                useCaseType,
                groundingSources,
                contextSummary,
                contextBundle);

            // ── Augmentar contexto com retrieval services ─────────────────
            groundingContext = await AugmentWithRetrievalAsync(
                groundingContext,
                request.Message,
                request.ContextScope,
                useCaseType,
                cancellationToken);

            string grounded;
            string? degradedReason = null;
            try
            {
                grounded = await externalAiRoutingPort.RouteQueryAsync(
                    groundingContext,
                    request.Message,
                    selectedProvider,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex,
                    "AI provider routing failed for conversation {ConversationId}. Persisting explicit degraded response.",
                    conversationId);

                degradedReason = AiMessage.ProviderUnavailableReason;
                grounded = BuildExplicitProviderUnavailableResponse(request.Message, groundingContext, ex.Message);
                selectedModel = DegradedModelName;
                selectedProvider = DegradedProviderId;
                isInternal = true;
            }

            var promptTokens = Math.Max(1, (request.Message.Length + groundingContext.Length) / 4);
            var completionTokens = Math.Max(1, grounded.Length / 4);
            var usedDeterministicFallback = grounded.StartsWith(AiMessage.DeterministicFallbackPrefix, StringComparison.OrdinalIgnoreCase);

            if (usedDeterministicFallback)
            {
                degradedReason ??= AiMessage.ProviderUnavailableReason;
                caveats ??= [];
                caveats.Add("Provider unavailable at request time. Explicit degraded response was persistida.");
                contextStrength = "partial";
                costClass = "none";
                escalationReason = "ProviderUnavailableFallback";
            }

            // Merge bundle caveats and context references
            if (contextBundle is not null)
            {
                if (contextBundle.Relations is { Count: > 0 })
                {
                    foreach (var rel in contextBundle.Relations)
                    {
                        var refStr = $"{rel.EntityType}:{rel.Name}";
                        if (!contextRefs.Contains(refStr))
                            contextRefs.Add(refStr);
                    }
                }
            }

            // Add caveat if context bundle was provided but could not be parsed
            if (bundleParseError)
            {
                caveats ??= [];
                caveats.Add("Context bundle could not be parsed; response may lack entity-specific detail.");
            }

            // ── Persistir mensagem do assistente ──────────────────────────
            conversation.RecordMessage(selectedModel, now);
            var assistantMsg = usedDeterministicFallback
                ? AiMessage.DegradedAssistantMessage(
                    conversationId,
                    grounded,
                    string.IsNullOrWhiteSpace(selectedModel) ? DegradedModelName : selectedModel,
                    string.IsNullOrWhiteSpace(selectedProvider) ? DegradedProviderId : selectedProvider,
                    promptTokens,
                    completionTokens,
                    applicableStrategy?.Name,
                    string.Join(",", groundingSources),
                    string.Join(",", contextRefs),
                    correlationId,
                    now)
                : AiMessage.AssistantMessage(
                    conversationId,
                    grounded,
                    selectedModel,
                    selectedProvider,
                    isInternal: isInternal,
                    promptTokens,
                    completionTokens,
                    appliedPolicyName: applicableStrategy?.Name,
                    string.Join(",", groundingSources),
                    string.Join(",", contextRefs),
                    correlationId,
                    now);
            await messageRepository.AddAsync(assistantMsg, cancellationToken);
            await conversationRepository.UpdateAsync(conversation, cancellationToken);

            // ── Registar auditoria de uso ─────────────────────────────────
            var usageEntry = AIUsageEntry.Record(
                currentUser.Id,
                currentUser.Name,
                request.PreferredModelId ?? Guid.Empty,
                selectedModel,
                selectedProvider,
                isInternal: isInternal,
                now,
                promptTokens,
                completionTokens,
                policyId: null,
                policyName: applicableStrategy?.Name,
                UsageResult.Allowed,
                request.ContextScope ?? string.Empty,
                clientType,
                correlationId,
                conversationId);

            await usageEntryRepository.AddAsync(usageEntry, cancellationToken);

            return new Response(
                conversationId,
                assistantMsg.Id.Value,
                assistantMsg.GetDisplayContent(),
                selectedModel,
                selectedProvider,
                IsInternalModel: isInternal,
                PromptTokens: promptTokens,
                CompletionTokens: completionTokens,
                AppliedPolicy: applicableStrategy?.Name,
                GroundingSources: groundingSources,
                ContextReferences: contextRefs,
                CorrelationId: correlationId,
                UseCaseType: useCaseType.ToString(),
                RoutingPath: usedDeterministicFallback ? "ProviderUnavailableFallback" : routingPath.ToString(),
                ConfidenceLevel: usedDeterministicFallback ? AIConfidenceLevel.Low.ToString() : (contextBundle is not null ? "High" : confidenceLevel),
                CostClass: costClass,
                RoutingRationale: usedDeterministicFallback
                    ? $"Provider unavailable. Explicit degraded response persisted. Original rationale: {routingRationale}"
                    : routingRationale,
                SourceWeightingSummary: sourceWeightingSummary,
                EscalationReason: escalationReason,
                ContextSummary: contextSummary,
                SuggestedNextSteps: suggestedSteps,
                ContextCaveats: caveats,
                ContextStrength: contextStrength,
                UserMessageId: userMessage.Id.Value,
                UserMessageTimestamp: userMessage.Timestamp,
                AssistantMessageTimestamp: assistantMsg.Timestamp,
                ResponseState: assistantMsg.GetResponseState(),
                IsDegraded: assistantMsg.IsDegradedResponse(),
                DegradedReason: degradedReason ?? assistantMsg.GetDegradedReason(),
                ConversationTitle: conversation.Title,
                ConversationMessageCount: conversation.MessageCount,
                ConversationLastMessageAt: conversation.LastMessageAt);
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

        /// <summary>
        /// Constrói contexto mínimo de grounding para envio ao provider.
        /// Inclui escopo, entidades referenciadas e bundle real quando disponível.
        /// </summary>
        private static string BuildGroundingContext(
            Command request,
            string persona,
            AIUseCaseType useCaseType,
            IReadOnlyList<string> groundingSources,
            string? contextSummary,
            ContextBundleData? contextBundle)
        {
            var lines = new List<string>
            {
                $"Persona: {persona}",
                $"UseCase: {useCaseType}",
                $"ContextScope: {request.ContextScope ?? "general"}",
                $"GroundingSources: {string.Join(", ", groundingSources)}"
            };

            if (!string.IsNullOrWhiteSpace(contextSummary))
                lines.Add($"ContextSummary: {contextSummary}");

            if (request.ServiceId.HasValue) lines.Add($"ServiceId: {request.ServiceId.Value}");
            if (request.ContractId.HasValue) lines.Add($"ContractId: {request.ContractId.Value}");
            if (request.IncidentId.HasValue) lines.Add($"IncidentId: {request.IncidentId.Value}");
            if (request.ChangeId.HasValue) lines.Add($"ChangeId: {request.ChangeId.Value}");
            if (request.TeamId.HasValue) lines.Add($"TeamId: {request.TeamId.Value}");
            if (request.DomainId.HasValue) lines.Add($"DomainId: {request.DomainId.Value}");

            if (contextBundle is not null)
            {
                lines.Add($"EntityType: {contextBundle.EntityType}");
                lines.Add($"EntityName: {contextBundle.EntityName}");

                if (!string.IsNullOrWhiteSpace(contextBundle.EntityStatus))
                    lines.Add($"EntityStatus: {contextBundle.EntityStatus}");

                if (!string.IsNullOrWhiteSpace(contextBundle.EntityDescription))
                    lines.Add($"EntityDescription: {contextBundle.EntityDescription}");

                if (contextBundle.Properties is { Count: > 0 })
                {
                    foreach (var prop in contextBundle.Properties)
                        lines.Add($"Property.{prop.Key}: {prop.Value}");
                }

                if (contextBundle.Relations is { Count: > 0 })
                {
                    foreach (var rel in contextBundle.Relations.Take(8))
                    {
                        lines.Add($"Relation.{rel.RelationType}: {rel.EntityType}:{rel.Name}" +
                                  (string.IsNullOrWhiteSpace(rel.Status) ? string.Empty : $" ({rel.Status})"));
                    }
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Augmenta o contexto de grounding com dados reais dos retrieval services.
        /// Consulta documentos, dados estruturados e telemetria conforme o ContextScope e UseCase.
        /// Falhas silenciosas — os retrieval services não devem bloquear o pipeline.
        /// </summary>
        private async Task<string> AugmentWithRetrievalAsync(
            string baseContext,
            string query,
            string? contextScope,
            AIUseCaseType useCaseType,
            CancellationToken cancellationToken)
        {
            var augmentation = new List<string>();

            // Document retrieval — knowledge sources, runbooks, documentation
            try
            {
                var docResult = await documentRetrievalService.SearchAsync(
                    new DocumentSearchRequest(query, MaxResults: 3), cancellationToken);

                if (docResult.Success && docResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedDocuments:");
                    foreach (var hit in docResult.Hits)
                        augmentation.Add($"  - [{hit.Classification}] {hit.Title}: {hit.Snippet}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Document retrieval augmentation failed — continuing without");
            }

            // Database retrieval — structured entity data (models, configurations)
            try
            {
                var scope = contextScope?.ToLowerInvariant() ?? string.Empty;
                var entityFilter = useCaseType switch
                {
                    AIUseCaseType.ContractExplanation or AIUseCaseType.ContractGeneration => "Contract",
                    AIUseCaseType.ServiceLookup or AIUseCaseType.DependencyReasoning => "Service",
                    _ => (string?)null
                };

                var dbResult = await databaseRetrievalService.SearchAsync(
                    new DatabaseSearchRequest(query, EntityType: entityFilter, MaxResults: 3),
                    cancellationToken);

                if (dbResult.Success && dbResult.Hits.Count > 0)
                {
                    augmentation.Add("RetrievedData:");
                    foreach (var hit in dbResult.Hits)
                        augmentation.Add($"  - [{hit.EntityType}] {hit.DisplayName}: {hit.Summary}");
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Database retrieval augmentation failed — continuing without");
            }

            // Telemetry retrieval — logs, traces (for incident/change analysis)
            if (useCaseType is AIUseCaseType.IncidentExplanation or AIUseCaseType.MitigationGuidance
                or AIUseCaseType.ChangeAnalysis or AIUseCaseType.FinOpsExplanation)
            {
                try
                {
                    var telResult = await telemetryRetrievalService.SearchAsync(
                        new TelemetrySearchRequest(query, MaxResults: 5), cancellationToken);

                    if (telResult.Success && telResult.Hits.Count > 0)
                    {
                        augmentation.Add("RetrievedTelemetry:");
                        foreach (var hit in telResult.Hits)
                            augmentation.Add(
                                $"  - [{hit.Severity}] {hit.ServiceName} @ {hit.Timestamp:u}: {(hit.Message.Length > 120 ? string.Concat(hit.Message.AsSpan(0, 117), "...") : hit.Message)}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Telemetry retrieval augmentation failed — continuing without");
                }
            }

            if (augmentation.Count == 0)
                return baseContext;

            return baseContext + "\n\n--- Retrieved Context ---\n" + string.Join("\n", augmentation);
        }

        /// <summary>
        /// Classifica o caso de uso baseado na query e contexto.
        /// Heurística baseada em palavras-chave — evolução futura com NLP.
        /// </summary>
        private static AIUseCaseType ClassifyUseCase(string query, string? contextScope)
        {
            var lower = query.ToLowerInvariant();
            var scope = contextScope?.ToLowerInvariant() ?? string.Empty;

            if (lower.Contains("contract") && lower.Contains("generat"))
                return AIUseCaseType.ContractGeneration;
            if (lower.Contains("contract") || scope.Contains("contracts"))
                return AIUseCaseType.ContractExplanation;
            if (lower.Contains("incident") || scope.Contains("incidents"))
                return AIUseCaseType.IncidentExplanation;
            if (lower.Contains("mitigat") || lower.Contains("runbook"))
                return AIUseCaseType.MitigationGuidance;
            if (lower.Contains("change") || lower.Contains("blast") || lower.Contains("deploy"))
                return AIUseCaseType.ChangeAnalysis;
            if (lower.Contains("summary") || lower.Contains("executive") || lower.Contains("overview"))
                return AIUseCaseType.ExecutiveSummary;
            if (lower.Contains("risk") || lower.Contains("compliance"))
                return AIUseCaseType.RiskComplianceExplanation;
            if (lower.Contains("cost") || lower.Contains("finops") || lower.Contains("waste"))
                return AIUseCaseType.FinOpsExplanation;
            if (lower.Contains("dependency") || lower.Contains("depend"))
                return AIUseCaseType.DependencyReasoning;
            if (lower.Contains("service") || scope.Contains("services"))
                return AIUseCaseType.ServiceLookup;

            return AIUseCaseType.General;
        }

        /// <summary>
        /// Seleciona modelo baseado em caso de uso, persona e caminho de roteamento.
        /// Retorna valores de fallback neutros — o provider real é resolvido pelo ModelRegistry ou AiRoutingOptions.
        /// </summary>
        private static (string Model, string Provider, bool IsInternal) SelectModel(
            AIUseCaseType useCaseType, string persona, AIRoutingPath routingPath)
        {
            // For external escalation paths, prefer external providers by returning an empty provider
            // so the routing adapter resolves from registry or options.
            if (routingPath == AIRoutingPath.ExternalEscalation)
                return (string.Empty, string.Empty, false);

            // All other paths: return empty values so the model catalog or routing options take precedence.
            return (string.Empty, string.Empty, true);
        }

        /// <summary>
        /// Resolve fontes de grounding com ponderação baseada no caso de uso.
        /// </summary>
        private static List<string> ResolveGroundingSources(
            string? contextScope,
            IReadOnlyList<AIKnowledgeSource> availableSources,
            AIUseCaseType useCaseType)
        {
            // Prioritize by use case when available sources exist
            if (availableSources.Count > 0)
            {
                var priorities = GetSourcePrioritiesByUseCase(useCaseType);
                var resolved = new List<string>();
                foreach (var sourceType in priorities)
                {
                    var source = availableSources.FirstOrDefault(s => s.SourceType == sourceType);
                    if (source is not null) resolved.Add(source.Name);
                }
                if (resolved.Count > 0) return resolved;
            }

            // Fallback to context scope
            if (string.IsNullOrWhiteSpace(contextScope))
                return ["Service Catalog", "Contract Registry"];

            var scopes = contextScope.Split(',', 20, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var sources = new List<string>();

            foreach (var scope in scopes)
            {
                sources.Add(scope.ToLowerInvariant() switch
                {
                    "services" => "Service Catalog",
                    "contracts" => "Contract Registry",
                    "incidents" => "Incident History",
                    "changes" => "Change Intelligence",
                    "runbooks" => "Runbook Library",
                    "dependencies" => "Dependency Graph",
                    "reliability" => "Reliability Metrics",
                    "governance" => "Governance Policies",
                    "policies" => "Access Policies",
                    "models" => "Model Registry",
                    "audit" => "Audit Trail",
                    "compliance" => "Compliance Records",
                    "risk" => "Risk Assessment",
                    "trends" => "Operational Trends",
                    _ => scope
                });
            }

            return sources.Count > 0 ? sources : ["Service Catalog", "Contract Registry"];
        }

        private static List<KnowledgeSourceType> GetSourcePrioritiesByUseCase(AIUseCaseType useCaseType) =>
            useCaseType switch
            {
                AIUseCaseType.ServiceLookup => [KnowledgeSourceType.Service, KnowledgeSourceType.Contract, KnowledgeSourceType.Documentation],
                AIUseCaseType.ContractExplanation => [KnowledgeSourceType.Contract, KnowledgeSourceType.Service, KnowledgeSourceType.SourceOfTruth],
                AIUseCaseType.ContractGeneration => [KnowledgeSourceType.Contract, KnowledgeSourceType.Service, KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Documentation],
                AIUseCaseType.IncidentExplanation => [KnowledgeSourceType.Incident, KnowledgeSourceType.Change, KnowledgeSourceType.Runbook, KnowledgeSourceType.TelemetrySummary],
                AIUseCaseType.MitigationGuidance => [KnowledgeSourceType.Runbook, KnowledgeSourceType.Incident, KnowledgeSourceType.Service, KnowledgeSourceType.TelemetrySummary],
                AIUseCaseType.ChangeAnalysis => [KnowledgeSourceType.Change, KnowledgeSourceType.Service, KnowledgeSourceType.Incident, KnowledgeSourceType.TelemetrySummary],
                AIUseCaseType.ExecutiveSummary => [KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Service, KnowledgeSourceType.TelemetrySummary],
                AIUseCaseType.RiskComplianceExplanation => [KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Service, KnowledgeSourceType.Documentation],
                AIUseCaseType.FinOpsExplanation => [KnowledgeSourceType.TelemetrySummary, KnowledgeSourceType.Service, KnowledgeSourceType.SourceOfTruth],
                AIUseCaseType.DependencyReasoning => [KnowledgeSourceType.Service, KnowledgeSourceType.Contract, KnowledgeSourceType.Change],
                _ => [KnowledgeSourceType.Service, KnowledgeSourceType.Contract, KnowledgeSourceType.SourceOfTruth, KnowledgeSourceType.Documentation]
            };

        /// <summary>
        /// Resolve referências de contexto baseadas nos IDs fornecidos na requisição.
        /// </summary>
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
                    if (!refs.Contains(scopeRef))
                        refs.Add(scopeRef);
                }
            }

            return refs;
        }

        /// <summary>
        /// Avalia pesos de fontes e nível de confiança para o caso de uso.
        /// </summary>
        private static (string WeightingSummary, string ConfidenceLevel) EvaluateSourceWeights(
            IReadOnlyList<AIKnowledgeSource> sources, AIUseCaseType useCaseType)
        {
            var priorities = GetSourcePrioritiesByUseCase(useCaseType);
            var matchCount = priorities.Count(p => sources.Any(s => s.SourceType == p));

            var weights = priorities
                .Where(p => sources.Any(s => s.SourceType == p))
                .Select(p => $"{p}:matched")
                .ToList();

            var summary = weights.Count > 0 ? string.Join(",", weights) : "no-matches";

            var confidence = matchCount >= 3
                ? AIConfidenceLevel.High.ToString()
                : matchCount >= 2
                    ? AIConfidenceLevel.Medium.ToString()
                    : matchCount >= 1
                        ? AIConfidenceLevel.Low.ToString()
                        : AIConfidenceLevel.Unknown.ToString();

            return (summary, confidence);
        }

        /// <summary>
        /// Constrói justificativa de roteamento legível.
        /// </summary>
        private static string BuildRoutingRationale(
            string persona, AIUseCaseType useCaseType, AIRoutingPath routingPath,
            string selectedModel, bool isInternal, string? strategyName, string confidenceLevel)
        {
            var parts = new List<string>
            {
                $"Use case: {useCaseType} (persona: {persona})",
                $"Routing: {routingPath}, model: {selectedModel} (internal: {isInternal})",
                $"Confidence: {confidenceLevel}"
            };

            if (strategyName is not null)
                parts.Add($"Strategy: {strategyName}");

            return string.Join(". ", parts) + ".";
        }

        /// <summary>
        /// Gera resposta grounded com dados reais do context bundle.
        /// O bundle contém informação real da entidade enviada pelo frontend.
        /// </summary>
        private static (string Response, string? ContextSummary, List<string>? Steps, List<string>? Caveats, string Strength)
            GenerateGroundedResponse(
                string message, string persona, AIUseCaseType useCaseType,
                List<string> groundingSources, ContextBundleData bundle)
        {
            var parts = new List<string>();
            var steps = new List<string>();
            var caveats = new List<string>();
            var contextProps = new List<string>();

            // ── Entity identity ─────────────────────────────────────────
            parts.Add($"**{bundle.EntityType}: {bundle.EntityName}**");
            if (!string.IsNullOrWhiteSpace(bundle.EntityStatus))
                parts.Add($"Status: {bundle.EntityStatus}");
            if (!string.IsNullOrWhiteSpace(bundle.EntityDescription))
                parts.Add(bundle.EntityDescription);

            // ── Properties (real data) ──────────────────────────────────
            if (bundle.Properties is { Count: > 0 })
            {
                var propLines = bundle.Properties
                    .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                    .Select(p => $"• {p.Key}: {p.Value}")
                    .ToList();

                if (propLines.Count > 0)
                {
                    parts.Add("\n" + string.Join("\n", propLines));
                    foreach (var prop in bundle.Properties.Keys)
                        contextProps.Add(prop);
                }
            }

            // ── Relations (real cross-entity data) ──────────────────────
            if (bundle.Relations is { Count: > 0 })
            {
                var grouped = bundle.Relations.GroupBy(r => r.RelationType);
                foreach (var group in grouped)
                {
                    parts.Add($"\n**{group.Key}:**");
                    foreach (var rel in group.Take(5))
                    {
                        var relInfo = $"• {rel.Name}";
                        if (!string.IsNullOrWhiteSpace(rel.Status))
                            relInfo += $" ({rel.Status})";
                        if (rel.Properties is { Count: > 0 })
                        {
                            var extras = rel.Properties
                                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                                .Take(3)
                                .Select(p => $"{p.Key}: {p.Value}");
                            relInfo += " — " + string.Join(", ", extras);
                        }
                        parts.Add(relInfo);
                    }
                    if (group.Count() > 5)
                        parts.Add($"  ... and {group.Count() - 5} more");
                }
            }

            // ── Entity-specific next steps ──────────────────────────────
            switch (bundle.EntityType.ToLowerInvariant())
            {
                case "service":
                    steps.Add("Review associated contracts for compliance");
                    steps.Add("Check dependency health and recent changes");
                    steps.Add("Verify operational readiness and monitoring");
                    break;
                case "contract":
                    steps.Add("Validate compatibility with registered consumers");
                    steps.Add("Review version history for breaking changes");
                    steps.Add("Verify ownership and governance status");
                    break;
                case "change":
                    steps.Add("Assess blast radius and affected services");
                    steps.Add("Validate evidence completeness before approval");
                    steps.Add("Check for correlated incidents post-deployment");
                    break;
                case "incident":
                    steps.Add("Correlate with recent changes and deployments");
                    steps.Add("Review applicable runbooks and mitigation steps");
                    steps.Add("Assess service dependencies and blast radius");
                    break;
            }

            // ── Caveats ─────────────────────────────────────────────────
            if (bundle.Caveats is { Count: > 0 })
                caveats.AddRange(bundle.Caveats);

            if (bundle.Relations is null || bundle.Relations.Count == 0)
                caveats.Add("Limited cross-entity context available");

            // ── Context strength assessment ──────────────────────────────
            var propCount = bundle.Properties?.Count ?? 0;
            var relCount = bundle.Relations?.Count ?? 0;
            var strength = (propCount, relCount) switch
            {
                ( >= 3, >= 2) => "strong",
                ( >= 2, >= 1) => "good",
                ( >= 1, _) => "partial",
                _ => "weak"
            };

            // ── Context summary ─────────────────────────────────────────
            var contextSummary = $"Consulted: {bundle.EntityType} ({bundle.EntityName})"
                + (contextProps.Count > 0 ? $" with {contextProps.Count} properties" : "")
                + (relCount > 0 ? $", {relCount} related entities" : "")
                + $". Sources: {string.Join(", ", groundingSources.Take(3))}";

            // ── Grounding source note ───────────────────────────────────
            parts.Add($"\n---\n*Grounded in {string.Join(", ", groundingSources.Take(3))}. Context strength: {strength}.*");

            return (string.Join("\n", parts), contextSummary, steps, caveats, strength);
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
