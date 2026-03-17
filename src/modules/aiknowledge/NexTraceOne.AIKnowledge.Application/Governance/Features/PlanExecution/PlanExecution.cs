using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.PlanExecution;

/// <summary>
/// Feature: PlanExecution — planeia execução de IA com seleção inteligente de modelo,
/// fontes e caminho de roteamento. Retorna plano explicável com metadados de governança.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class PlanExecution
{
    /// <summary>Comando de planeamento de execução de IA.</summary>
    public sealed record Command(
        string InputQuery,
        string? Persona,
        string? ContextScope,
        string ClientType,
        Guid? PreferredModelId,
        Guid? ServiceId,
        Guid? ContractId,
        Guid? IncidentId) : ICommand<Response>;

    /// <summary>Valida o comando de planeamento de execução.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InputQuery).NotEmpty().MaximumLength(10_000);
            RuleFor(x => x.ClientType).NotEmpty();
        }
    }

    /// <summary>Handler que planeia a execução de IA com roteamento inteligente.</summary>
    public sealed class Handler(
        IAiRoutingStrategyRepository strategyRepository,
        IAiKnowledgeSourceRepository knowledgeSourceRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var correlationId = Guid.NewGuid().ToString();
            var persona = request.Persona ?? "Engineer";

            // ── Classificar caso de uso ──────────────────────────────────
            var useCaseType = ClassifyUseCase(request.InputQuery, request.ContextScope);

            // ── Selecionar estratégia de roteamento aplicável ────────────
            var strategies = await strategyRepository.ListAsync(isActive: true, cancellationToken);
            var applicableStrategy = strategies
                .Where(s => s.IsApplicable(persona, useCaseType.ToString(), request.ClientType))
                .OrderBy(s => s.Priority)
                .FirstOrDefault();

            var routingPath = applicableStrategy?.PreferredPath ?? AIRoutingPath.InternalOnly;
            var allowExternal = applicableStrategy?.AllowExternalEscalation ?? false;

            // ── Selecionar modelo ────────────────────────────────────────
            var (selectedModel, selectedProvider, isInternal) = SelectModel(
                useCaseType, persona, routingPath);

            // ── Resolver fontes e pesos ──────────────────────────────────
            var sources = await knowledgeSourceRepository.ListAsync(
                sourceType: null, isActive: true, cancellationToken);

            var (selectedSources, weightingSummary) = ResolveSourceWeights(
                sources, useCaseType, persona);

            // ── Avaliar confiança e custo ────────────────────────────────
            var confidenceLevel = EvaluateConfidence(selectedSources, sources.Count);
            var costClass = EvaluateCostClass(isInternal, useCaseType);
            var escalationReason = isInternal ? AIEscalationReason.None : AIEscalationReason.ComplexityRequiresAdvancedModel;

            // ── Construir justificativa ──────────────────────────────────
            var rationale = BuildRationale(
                persona, useCaseType, routingPath, selectedModel,
                isInternal, applicableStrategy?.Name, confidenceLevel);

            var policyDecision = applicableStrategy is not null
                ? $"Strategy '{applicableStrategy.Name}' applied (priority {applicableStrategy.Priority})"
                : "Default internal-only routing applied (no specific strategy matched)";

            return new Response(
                correlationId,
                useCaseType.ToString(),
                selectedModel,
                selectedProvider,
                isInternal,
                routingPath.ToString(),
                selectedSources,
                weightingSummary,
                policyDecision,
                costClass,
                rationale,
                confidenceLevel.ToString(),
                escalationReason.ToString(),
                applicableStrategy?.Id.Value);
        }

        /// <summary>
        /// Classifica o caso de uso baseado na query e escopo de contexto.
        /// Heurística baseada em palavras-chave — evolução futura com NLP.
        /// </summary>
        private static AIUseCaseType ClassifyUseCase(string query, string? contextScope)
        {
            var lowerQuery = query.ToLowerInvariant();
            var lowerScope = contextScope?.ToLowerInvariant() ?? string.Empty;

            if (lowerQuery.Contains("contract") && lowerQuery.Contains("generat"))
                return AIUseCaseType.ContractGeneration;
            if (lowerQuery.Contains("contract") || lowerScope.Contains("contracts"))
                return AIUseCaseType.ContractExplanation;
            if (lowerQuery.Contains("incident") || lowerScope.Contains("incidents"))
                return AIUseCaseType.IncidentExplanation;
            if (lowerQuery.Contains("mitigat") || lowerQuery.Contains("runbook"))
                return AIUseCaseType.MitigationGuidance;
            if (lowerQuery.Contains("change") || lowerQuery.Contains("blast") || lowerQuery.Contains("deploy"))
                return AIUseCaseType.ChangeAnalysis;
            if (lowerQuery.Contains("summary") || lowerQuery.Contains("executive") || lowerQuery.Contains("overview"))
                return AIUseCaseType.ExecutiveSummary;
            if (lowerQuery.Contains("risk") || lowerQuery.Contains("compliance"))
                return AIUseCaseType.RiskComplianceExplanation;
            if (lowerQuery.Contains("cost") || lowerQuery.Contains("finops") || lowerQuery.Contains("waste"))
                return AIUseCaseType.FinOpsExplanation;
            if (lowerQuery.Contains("dependency") || lowerQuery.Contains("depend"))
                return AIUseCaseType.DependencyReasoning;
            if (lowerQuery.Contains("service") || lowerScope.Contains("services"))
                return AIUseCaseType.ServiceLookup;

            return AIUseCaseType.General;
        }

        /// <summary>
        /// Seleciona modelo baseado em caso de uso, persona e caminho de roteamento.
        /// Stub: retorna modelo interno por padrão — evolução com Model Registry lookup.
        /// </summary>
        private static (string Model, string Provider, bool IsInternal) SelectModel(
            AIUseCaseType useCaseType, string persona, AIRoutingPath routingPath)
        {
            // Casos que podem beneficiar de modelo mais capaz
            if (routingPath == AIRoutingPath.ExternalEscalation &&
                useCaseType is AIUseCaseType.ContractGeneration or AIUseCaseType.ChangeAnalysis)
            {
                return ("NexTrace-Advanced-v1", "Internal-Advanced", true);
            }

            // Executivos usam modelo otimizado para síntese
            if (persona.Equals("Executive", StringComparison.OrdinalIgnoreCase) ||
                persona.Equals("Product", StringComparison.OrdinalIgnoreCase))
            {
                return ("NexTrace-Summary-v1", "Internal", true);
            }

            return ("NexTrace-Internal-v1", "Internal", true);
        }

        /// <summary>
        /// Resolve fontes e pesos baseados no caso de uso e persona.
        /// </summary>
        private static (string SelectedSources, string WeightingSummary) ResolveSourceWeights(
            IReadOnlyList<AIKnowledgeSource> availableSources, AIUseCaseType useCaseType, string persona)
        {
            var sourceNames = new List<string>();
            var weights = new List<string>();

            // Definir fontes prioritárias por caso de uso
            var priorities = GetSourcePriorities(useCaseType);

            foreach (var (sourceType, weight) in priorities)
            {
                var source = availableSources.FirstOrDefault(s => s.SourceType == sourceType);
                if (source is not null)
                {
                    sourceNames.Add(source.Name);
                    weights.Add($"{source.SourceType}:{weight}%");
                }
            }

            if (sourceNames.Count == 0)
            {
                sourceNames.Add("General Knowledge Base");
                weights.Add("general:100%");
            }

            return (string.Join(",", sourceNames), string.Join(",", weights));
        }

        /// <summary>
        /// Retorna prioridades de fontes para cada caso de uso.
        /// </summary>
        private static List<(KnowledgeSourceType SourceType, int Weight)> GetSourcePriorities(
            AIUseCaseType useCaseType) => useCaseType switch
        {
            AIUseCaseType.ServiceLookup => [
                (KnowledgeSourceType.Service, 60),
                (KnowledgeSourceType.Contract, 25),
                (KnowledgeSourceType.Documentation, 15)],
            AIUseCaseType.ContractExplanation => [
                (KnowledgeSourceType.Contract, 55),
                (KnowledgeSourceType.Service, 25),
                (KnowledgeSourceType.SourceOfTruth, 20)],
            AIUseCaseType.ContractGeneration => [
                (KnowledgeSourceType.Contract, 50),
                (KnowledgeSourceType.Service, 25),
                (KnowledgeSourceType.SourceOfTruth, 15),
                (KnowledgeSourceType.Documentation, 10)],
            AIUseCaseType.IncidentExplanation => [
                (KnowledgeSourceType.Incident, 40),
                (KnowledgeSourceType.Change, 25),
                (KnowledgeSourceType.Runbook, 20),
                (KnowledgeSourceType.TelemetrySummary, 15)],
            AIUseCaseType.MitigationGuidance => [
                (KnowledgeSourceType.Runbook, 40),
                (KnowledgeSourceType.Incident, 30),
                (KnowledgeSourceType.Service, 15),
                (KnowledgeSourceType.TelemetrySummary, 15)],
            AIUseCaseType.ChangeAnalysis => [
                (KnowledgeSourceType.Change, 45),
                (KnowledgeSourceType.Service, 25),
                (KnowledgeSourceType.Incident, 20),
                (KnowledgeSourceType.TelemetrySummary, 10)],
            AIUseCaseType.ExecutiveSummary => [
                (KnowledgeSourceType.SourceOfTruth, 40),
                (KnowledgeSourceType.Service, 30),
                (KnowledgeSourceType.TelemetrySummary, 30)],
            AIUseCaseType.RiskComplianceExplanation => [
                (KnowledgeSourceType.SourceOfTruth, 40),
                (KnowledgeSourceType.Service, 30),
                (KnowledgeSourceType.Documentation, 30)],
            AIUseCaseType.FinOpsExplanation => [
                (KnowledgeSourceType.TelemetrySummary, 45),
                (KnowledgeSourceType.Service, 35),
                (KnowledgeSourceType.SourceOfTruth, 20)],
            AIUseCaseType.DependencyReasoning => [
                (KnowledgeSourceType.Service, 45),
                (KnowledgeSourceType.Contract, 35),
                (KnowledgeSourceType.Change, 20)],
            _ => [
                (KnowledgeSourceType.Service, 35),
                (KnowledgeSourceType.Contract, 25),
                (KnowledgeSourceType.SourceOfTruth, 20),
                (KnowledgeSourceType.Documentation, 20)]
        };

        private static AIConfidenceLevel EvaluateConfidence(string selectedSources, int totalAvailable)
        {
            var sourceCount = selectedSources.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            if (sourceCount >= 3 && totalAvailable >= 3) return AIConfidenceLevel.High;
            if (sourceCount >= 2) return AIConfidenceLevel.Medium;
            if (sourceCount >= 1) return AIConfidenceLevel.Low;
            return AIConfidenceLevel.Unknown;
        }

        private static string EvaluateCostClass(bool isInternal, AIUseCaseType useCaseType)
        {
            if (isInternal)
                return useCaseType is AIUseCaseType.ContractGeneration or AIUseCaseType.ChangeAnalysis
                    ? "medium"
                    : "low";
            return "high";
        }

        private static string BuildRationale(
            string persona, AIUseCaseType useCaseType, AIRoutingPath routingPath,
            string selectedModel, bool isInternal, string? strategyName,
            AIConfidenceLevel confidenceLevel)
        {
            var parts = new List<string>
            {
                $"Use case classified as '{useCaseType}' for persona '{persona}'",
                $"Routing path: {routingPath} (model: {selectedModel}, internal: {isInternal})",
                $"Confidence level: {confidenceLevel}"
            };

            if (strategyName is not null)
                parts.Add($"Applied strategy: '{strategyName}'");
            else
                parts.Add("No specific strategy matched; default internal routing applied");

            return string.Join(". ", parts) + ".";
        }
    }

    /// <summary>Resposta do planeamento de execução de IA.</summary>
    public sealed record Response(
        string CorrelationId,
        string UseCaseType,
        string SelectedModel,
        string SelectedProvider,
        bool IsInternal,
        string RoutingPath,
        string SelectedSources,
        string SourceWeightingSummary,
        string PolicyDecision,
        string EstimatedCostClass,
        string RationaleSummary,
        string ConfidenceLevel,
        string EscalationReason,
        Guid? AppliedStrategyId);
}
