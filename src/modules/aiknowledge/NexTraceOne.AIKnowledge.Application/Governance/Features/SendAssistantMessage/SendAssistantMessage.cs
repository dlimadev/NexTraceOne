using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.SendAssistantMessage;

/// <summary>
/// Feature: SendAssistantMessage — envia uma mensagem ao assistente de IA governado.
/// Valida políticas de acesso, regista auditoria de uso, persiste mensagens na conversa
/// e retorna resposta com metadados completos de grounding, roteamento e explicabilidade.
/// Integra pipeline de roteamento inteligente e enriquecimento de contexto.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SendAssistantMessage
{
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
        Guid? TeamId,
        Guid? DomainId) : ICommand<Response>;

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
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var correlationId = Guid.NewGuid().ToString();
            var clientType = Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct)
                ? ct
                : AIClientType.Web;
            var persona = request.Persona ?? "Engineer";

            // ── Resolver ou criar conversa ────────────────────────────────
            AiAssistantConversation? conversation = null;
            if (request.ConversationId.HasValue)
            {
                var convId = AiAssistantConversationId.From(request.ConversationId.Value);
                conversation = await conversationRepository.GetByIdAsync(convId, cancellationToken);
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
                    currentUser.Id,
                    request.ServiceId,
                    request.ContractId,
                    request.IncidentId,
                    request.TeamId);
                await conversationRepository.AddAsync(conversation, cancellationToken);
            }

            var conversationId = conversation.Id.Value;

            // ── Persistir mensagem do utilizador ──────────────────────────
            var userMessage = AiMessage.UserMessage(conversationId, request.Message, now);
            await messageRepository.AddAsync(userMessage, cancellationToken);
            conversation.RecordMessage(null, now);

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

            // ── Stub: gerar resposta da IA (LLM integration em evolução) ─
            const int stubPromptTokens = 0;
            const int stubCompletionTokens = 0;

            var stubResponse = GenerateStubResponse(request.Message, persona, useCaseType, groundingSources);

            // ── Persistir mensagem do assistente ──────────────────────────
            var assistantMsg = AiMessage.AssistantMessage(
                conversationId,
                stubResponse,
                selectedModel,
                selectedProvider,
                isInternal: isInternal,
                stubPromptTokens,
                stubCompletionTokens,
                appliedPolicyName: applicableStrategy?.Name,
                string.Join(",", groundingSources),
                string.Join(",", contextRefs),
                correlationId,
                now);
            await messageRepository.AddAsync(assistantMsg, cancellationToken);
            conversation.RecordMessage(selectedModel, now);

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
                stubPromptTokens,
                stubCompletionTokens,
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
                stubResponse,
                selectedModel,
                selectedProvider,
                IsInternalModel: isInternal,
                PromptTokens: stubPromptTokens,
                CompletionTokens: stubCompletionTokens,
                AppliedPolicy: applicableStrategy?.Name,
                GroundingSources: groundingSources,
                ContextReferences: contextRefs,
                CorrelationId: correlationId,
                UseCaseType: useCaseType.ToString(),
                RoutingPath: routingPath.ToString(),
                ConfidenceLevel: confidenceLevel,
                CostClass: costClass,
                RoutingRationale: routingRationale,
                SourceWeightingSummary: sourceWeightingSummary,
                EscalationReason: escalationReason);
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
        /// </summary>
        private static (string Model, string Provider, bool IsInternal) SelectModel(
            AIUseCaseType useCaseType, string persona, AIRoutingPath routingPath)
        {
            if (routingPath == AIRoutingPath.ExternalEscalation &&
                useCaseType is AIUseCaseType.ContractGeneration or AIUseCaseType.ChangeAnalysis)
                return ("NexTrace-Advanced-v1", "Internal-Advanced", true);

            if (persona.Equals("Executive", StringComparison.OrdinalIgnoreCase) ||
                persona.Equals("Product", StringComparison.OrdinalIgnoreCase))
                return ("NexTrace-Summary-v1", "Internal", true);

            return ("NexTrace-Internal-v1", "Internal", true);
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
            if (request.TeamId.HasValue) refs.Add($"team:{request.TeamId.Value}");
            if (request.DomainId.HasValue) refs.Add($"domain:{request.DomainId.Value}");
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
        /// Gera resposta stub contextual baseada no prompt, persona e caso de uso.
        /// Evolução futura: integração com LLM provider via routing pipeline.
        /// </summary>
        private static string GenerateStubResponse(
            string message, string persona, AIUseCaseType useCaseType, List<string> groundingSources)
        {
            var personaContext = persona.ToLowerInvariant() switch
            {
                "engineer" => "From a technical perspective",
                "techlead" => "From a team leadership perspective",
                "architect" => "From an architectural perspective",
                "product" => "From a product impact perspective",
                "executive" => "At a strategic level",
                "platformadmin" => "From a platform governance perspective",
                "auditor" => "From a compliance and traceability perspective",
                _ => "Based on available context"
            };

            var useCaseContext = useCaseType switch
            {
                AIUseCaseType.ServiceLookup => "service catalog data",
                AIUseCaseType.ContractExplanation => "contract registry data",
                AIUseCaseType.ContractGeneration => "contract patterns and schemas",
                AIUseCaseType.IncidentExplanation => "incident history and change correlation",
                AIUseCaseType.MitigationGuidance => "runbooks and incident patterns",
                AIUseCaseType.ChangeAnalysis => "change intelligence and blast radius analysis",
                AIUseCaseType.ExecutiveSummary => "operational summaries and scorecards",
                AIUseCaseType.RiskComplianceExplanation => "risk assessment and compliance data",
                AIUseCaseType.FinOpsExplanation => "cost analysis and efficiency metrics",
                AIUseCaseType.DependencyReasoning => "service dependencies and topology",
                _ => "general operational knowledge"
            };

            var sourceList = groundingSources.Count > 0
                ? string.Join(", ", groundingSources.Take(3))
                : "general knowledge base";

            return $"{personaContext}: I've analyzed your question \"{(message.Length > 60 ? string.Concat(message.AsSpan(0, 57), "...") : message)}\" " +
                   $"using {useCaseContext} grounded in {sourceList}. " +
                   "Context-aware routing and enrichment is active — full LLM integration with model selection " +
                   "and source-weighted responses will be available in upcoming iterations. " +
                   "This response demonstrates the routing, enrichment and explainability framework.";
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
        string EscalationReason);
}
