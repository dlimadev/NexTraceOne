using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListKnowledgeSourceWeights;

/// <summary>
/// Feature: ListKnowledgeSourceWeights — lista pesos configurados de fontes
/// de conhecimento por caso de uso. Consulta configurações persistidas no banco,
/// com fallback aos pesos padrão quando nenhuma configuração estiver presente.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListKnowledgeSourceWeights
{
    /// <summary>Query de listagem de pesos de fontes de conhecimento.</summary>
    public sealed record Query(string? UseCaseType) : IQuery<Response>;

    /// <summary>Handler que lista pesos de fontes por caso de uso.</summary>
    public sealed class Handler(
        IAiKnowledgeSourceWeightRepository weightRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var useCaseFilter = request.UseCaseType is not null
                ? Enum.TryParse<AIUseCaseType>(request.UseCaseType, ignoreCase: true, out var parsed)
                    ? parsed
                    : (AIUseCaseType?)null
                : null;

            // Consultar configurações persistidas
            var persisted = await weightRepository.ListAsync(
                useCaseFilter, isActive: true, cancellationToken);

            // Se não existem configurações persistidas, retornar os pesos padrão
            // (comportamento de bootstrap: novos ambientes têm pesos razoáveis sem configuração manual)
            var items = persisted.Count > 0
                ? persisted.Select(w => new SourceWeightItem(
                    w.SourceType.ToString(),
                    w.UseCaseType.ToString(),
                    w.Relevance.ToString(),
                    w.Weight,
                    w.TrustLevel)).ToList()
                : BuildDefaultWeights(useCaseFilter);

            return new Response(items, items.Count);
        }

        /// <summary>
        /// Constrói pesos padrão de fontes por caso de uso.
        /// Usado como fallback quando nenhuma configuração persistida existe.
        /// </summary>
        private static List<SourceWeightItem> BuildDefaultWeights(AIUseCaseType? useCaseFilter)
        {
            var weights = new List<SourceWeightItem>();

            AddWeights(weights, AIUseCaseType.ServiceLookup,
                (KnowledgeSourceType.Service, 60, "Primary"),
                (KnowledgeSourceType.Contract, 25, "Secondary"),
                (KnowledgeSourceType.Documentation, 15, "Supplementary"));

            AddWeights(weights, AIUseCaseType.ContractExplanation,
                (KnowledgeSourceType.Contract, 55, "Primary"),
                (KnowledgeSourceType.Service, 25, "Secondary"),
                (KnowledgeSourceType.SourceOfTruth, 20, "Secondary"));

            AddWeights(weights, AIUseCaseType.ContractGeneration,
                (KnowledgeSourceType.Contract, 50, "Primary"),
                (KnowledgeSourceType.Service, 25, "Secondary"),
                (KnowledgeSourceType.SourceOfTruth, 15, "Supplementary"),
                (KnowledgeSourceType.Documentation, 10, "Supplementary"));

            AddWeights(weights, AIUseCaseType.IncidentExplanation,
                (KnowledgeSourceType.Incident, 40, "Primary"),
                (KnowledgeSourceType.Change, 25, "Secondary"),
                (KnowledgeSourceType.Runbook, 20, "Secondary"),
                (KnowledgeSourceType.TelemetrySummary, 15, "Supplementary"));

            AddWeights(weights, AIUseCaseType.MitigationGuidance,
                (KnowledgeSourceType.Runbook, 40, "Primary"),
                (KnowledgeSourceType.Incident, 30, "Primary"),
                (KnowledgeSourceType.Service, 15, "Secondary"),
                (KnowledgeSourceType.TelemetrySummary, 15, "Supplementary"));

            AddWeights(weights, AIUseCaseType.ChangeAnalysis,
                (KnowledgeSourceType.Change, 45, "Primary"),
                (KnowledgeSourceType.Service, 25, "Secondary"),
                (KnowledgeSourceType.Incident, 20, "Secondary"),
                (KnowledgeSourceType.TelemetrySummary, 10, "Supplementary"));

            AddWeights(weights, AIUseCaseType.ExecutiveSummary,
                (KnowledgeSourceType.SourceOfTruth, 40, "Primary"),
                (KnowledgeSourceType.Service, 30, "Secondary"),
                (KnowledgeSourceType.TelemetrySummary, 30, "Secondary"));

            AddWeights(weights, AIUseCaseType.RiskComplianceExplanation,
                (KnowledgeSourceType.SourceOfTruth, 40, "Primary"),
                (KnowledgeSourceType.Service, 30, "Secondary"),
                (KnowledgeSourceType.Documentation, 30, "Secondary"));

            AddWeights(weights, AIUseCaseType.FinOpsExplanation,
                (KnowledgeSourceType.TelemetrySummary, 45, "Primary"),
                (KnowledgeSourceType.Service, 35, "Secondary"),
                (KnowledgeSourceType.SourceOfTruth, 20, "Supplementary"));

            AddWeights(weights, AIUseCaseType.DependencyReasoning,
                (KnowledgeSourceType.Service, 45, "Primary"),
                (KnowledgeSourceType.Contract, 35, "Primary"),
                (KnowledgeSourceType.Change, 20, "Secondary"));

            AddWeights(weights, AIUseCaseType.General,
                (KnowledgeSourceType.Service, 35, "Primary"),
                (KnowledgeSourceType.Contract, 25, "Secondary"),
                (KnowledgeSourceType.SourceOfTruth, 20, "Secondary"),
                (KnowledgeSourceType.Documentation, 20, "Supplementary"));

            return useCaseFilter.HasValue
                ? weights.Where(w => w.UseCaseType == useCaseFilter.Value.ToString()).ToList()
                : weights;
        }

        private static void AddWeights(
            List<SourceWeightItem> list,
            AIUseCaseType useCaseType,
            params (KnowledgeSourceType sourceType, int weight, string relevance)[] entries)
        {
            foreach (var (sourceType, weight, relevance) in entries)
            {
                list.Add(new SourceWeightItem(
                    sourceType.ToString(),
                    useCaseType.ToString(),
                    relevance,
                    weight,
                    4));
            }
        }
    }

    /// <summary>Resposta da listagem de pesos de fontes de conhecimento.</summary>
    public sealed record Response(
        IReadOnlyList<SourceWeightItem> Items,
        int TotalCount);

    /// <summary>Item de peso de fonte de conhecimento.</summary>
    public sealed record SourceWeightItem(
        string SourceType,
        string UseCaseType,
        string Relevance,
        int Weight,
        int TrustLevel);
}

