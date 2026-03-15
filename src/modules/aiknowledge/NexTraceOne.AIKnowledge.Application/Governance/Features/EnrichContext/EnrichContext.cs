using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AiGovernance.Application.Abstractions;
using NexTraceOne.AiGovernance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AiGovernance.Application.Features.EnrichContext;

/// <summary>
/// Feature: EnrichContext — executa pipeline de enriquecimento de contexto para IA.
/// Consulta fontes prioritárias baseadas no caso de uso e persona, agrega contexto
/// ponderado e retorna bundle de conhecimento com metadados de confiança.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// Stub: retorna contexto simulado — integração real com fontes em evolução futura.
/// </summary>
public static class EnrichContext
{
    /// <summary>Comando de enriquecimento de contexto para consulta de IA.</summary>
    public sealed record Command(
        string InputQuery,
        string? Persona,
        string? UseCaseType,
        string? TargetScope,
        Guid? ServiceId,
        Guid? ContractId,
        Guid? IncidentId) : ICommand<Response>;

    /// <summary>Valida o comando de enriquecimento de contexto.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InputQuery).NotEmpty().MaximumLength(10_000);
        }
    }

    /// <summary>Handler que executa o pipeline de enriquecimento de contexto.</summary>
    public sealed class Handler(
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
            var useCaseType = request.UseCaseType is not null &&
                              Enum.TryParse<AIUseCaseType>(request.UseCaseType, ignoreCase: true, out var parsed)
                ? parsed
                : ClassifyFromQuery(request.InputQuery);

            // ── Obter fontes ativas ──────────────────────────────────────
            var sources = await knowledgeSourceRepository.ListAsync(
                sourceType: null, isActive: true, cancellationToken);

            // ── Resolver contexto por caso de uso ────────────────────────
            var queriedSources = new List<string>();
            var resolvedSources = new List<string>();
            var contextItems = new List<ContextItem>();

            var priorities = GetSourcePriorities(useCaseType);
            foreach (var (sourceType, weight) in priorities)
            {
                var source = sources.FirstOrDefault(s => s.SourceType == sourceType);
                if (source is not null)
                {
                    queriedSources.Add(source.Name);
                    resolvedSources.Add(source.Name);

                    // Stub: gerar itens de contexto simulados
                    contextItems.Add(new ContextItem(
                        source.SourceType.ToString(),
                        source.Name,
                        $"Contextual data from {source.Name} for '{useCaseType}' analysis",
                        weight,
                        source.Priority));
                }
            }

            // ── Adicionar referências de entidade ────────────────────────
            var entityHints = new List<string>();
            if (request.ServiceId.HasValue) entityHints.Add($"service:{request.ServiceId.Value}");
            if (request.ContractId.HasValue) entityHints.Add($"contract:{request.ContractId.Value}");
            if (request.IncidentId.HasValue) entityHints.Add($"incident:{request.IncidentId.Value}");

            // ── Avaliar confiança ────────────────────────────────────────
            var confidenceLevel = resolvedSources.Count >= 3
                ? AIConfidenceLevel.High
                : resolvedSources.Count >= 2
                    ? AIConfidenceLevel.Medium
                    : resolvedSources.Count >= 1
                        ? AIConfidenceLevel.Low
                        : AIConfidenceLevel.Unknown;

            var contextSummary = resolvedSources.Count > 0
                ? $"Enriched with {resolvedSources.Count} source(s) for {useCaseType}: {string.Join(", ", resolvedSources)}"
                : "No relevant sources available for enrichment";

            return new Response(
                correlationId,
                useCaseType.ToString(),
                persona,
                string.Join(",", queriedSources),
                string.Join(",", resolvedSources),
                contextItems,
                entityHints,
                contextItems.Count,
                confidenceLevel.ToString(),
                contextSummary,
                12);
        }

        private static AIUseCaseType ClassifyFromQuery(string query)
        {
            var lower = query.ToLowerInvariant();
            if (lower.Contains("contract") && lower.Contains("generat")) return AIUseCaseType.ContractGeneration;
            if (lower.Contains("contract")) return AIUseCaseType.ContractExplanation;
            if (lower.Contains("incident")) return AIUseCaseType.IncidentExplanation;
            if (lower.Contains("mitigat") || lower.Contains("runbook")) return AIUseCaseType.MitigationGuidance;
            if (lower.Contains("change") || lower.Contains("deploy")) return AIUseCaseType.ChangeAnalysis;
            if (lower.Contains("summary") || lower.Contains("executive")) return AIUseCaseType.ExecutiveSummary;
            if (lower.Contains("risk") || lower.Contains("compliance")) return AIUseCaseType.RiskComplianceExplanation;
            if (lower.Contains("cost") || lower.Contains("finops")) return AIUseCaseType.FinOpsExplanation;
            if (lower.Contains("dependency")) return AIUseCaseType.DependencyReasoning;
            if (lower.Contains("service")) return AIUseCaseType.ServiceLookup;
            return AIUseCaseType.General;
        }

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
    }

    /// <summary>Resposta do pipeline de enriquecimento de contexto.</summary>
    public sealed record Response(
        string CorrelationId,
        string UseCaseType,
        string Persona,
        string QueriedSources,
        string ResolvedSources,
        IReadOnlyList<ContextItem> ContextItems,
        IReadOnlyList<string> EntityHints,
        int TotalContextItems,
        string ConfidenceLevel,
        string ContextSummary,
        int ProcessingTimeMs);

    /// <summary>Item de contexto individual agregado pelo pipeline.</summary>
    public sealed record ContextItem(
        string SourceType,
        string SourceName,
        string Summary,
        int Weight,
        int SourcePriority);
}
