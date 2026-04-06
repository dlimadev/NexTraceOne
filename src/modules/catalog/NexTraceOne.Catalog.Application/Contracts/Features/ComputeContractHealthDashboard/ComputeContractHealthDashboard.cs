using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ComputeContractHealthDashboard;

/// <summary>
/// Feature: ComputeContractHealthDashboard — calcula métricas de saúde dos contratos.
/// Agrega dados de qualidade: percentagem com exemplos, com entidades canónicas linkadas,
/// deprecated com consumidores activos, e os contratos com mais violações de regras.
/// Estrutura VSA: Query + Handler + Response em arquivo único.
/// </summary>
public static class ComputeContractHealthDashboard
{
    /// <summary>Query para o dashboard de saúde dos contratos.</summary>
    public sealed record Query(
        string? Domain = null,
        string? ContractType = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>
    /// Handler que agrega métricas de saúde dos contratos.
    /// Carrega todas as versões mais recentes por contrato distinto e calcula:
    /// percentagem com exemplos JSON, com entidades canónicas, deprecated e score global.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Obter sumário agregado
            var summary = await repository.GetSummaryAsync(cancellationToken);

            // Pesquisar versões mais recentes com filtros
            ContractProtocol? protocol = null;
            if (!string.IsNullOrEmpty(request.ContractType)
                && Enum.TryParse<ContractProtocol>(request.ContractType, ignoreCase: true, out var parsed))
                protocol = parsed;

            var (items, totalCount) = await repository.SearchAsync(
                protocol,
                null,
                null,
                request.Domain,
                request.Page,
                request.PageSize,
                cancellationToken);

            // Calcular métricas
            var withExamples = items.Count(v =>
                v.SpecContent.Contains("\"example\":", StringComparison.OrdinalIgnoreCase)
                || v.SpecContent.Contains("example:", StringComparison.OrdinalIgnoreCase));

            var withCanonical = items.Count(v =>
                v.SpecContent.Contains("#/components/schemas/", StringComparison.OrdinalIgnoreCase));

            var deprecatedCount = items.Count(v =>
                v.LifecycleState == ContractLifecycleState.Deprecated);

            var withViolations = items
                .Where(v => v.RuleViolations.Count > 0)
                .OrderByDescending(v => v.RuleViolations.Count)
                .Take(10)
                .Select(v => new TopViolation(
                    v.Id.Value,
                    v.SemVer,
                    v.RuleViolations.Count,
                    v.RuleViolations.GroupBy(r => r.RuleName)
                        .Select(g => g.Key)
                        .Take(3)
                        .ToList()
                        .AsReadOnly()))
                .ToList()
                .AsReadOnly();

            // Score de saúde 0–100
            var percentWithExamples = items.Count > 0 ? (double)withExamples / items.Count * 100 : 0;
            var percentWithCanonical = items.Count > 0 ? (double)withCanonical / items.Count * 100 : 0;
            var percentActive = summary.TotalVersions > 0
                ? (double)(summary.TotalVersions - summary.DeprecatedCount) / summary.TotalVersions * 100
                : 100;

            var healthScore = (int)Math.Round((percentWithExamples * 0.3 + percentWithCanonical * 0.3 + percentActive * 0.4));

            return new Response(
                summary.TotalVersions,
                summary.DistinctContracts,
                summary.DeprecatedCount,
                totalCount,
                (int)Math.Round(percentWithExamples),
                (int)Math.Round(percentWithCanonical),
                withViolations,
                Math.Clamp(healthScore, 0, 100));
        }
    }

    /// <summary>Contrato com maior número de violações.</summary>
    public sealed record TopViolation(
        Guid ContractVersionId,
        string SemVer,
        int ViolationCount,
        IReadOnlyList<string> TopRuleIds);

    /// <summary>Resposta do dashboard de saúde dos contratos.</summary>
    public sealed record Response(
        int TotalContractVersions,
        int DistinctContracts,
        int DeprecatedVersions,
        int FilteredCount,
        int PercentWithExamples,
        int PercentWithCanonicalEntities,
        IReadOnlyList<TopViolation> TopViolations,
        int HealthScore);
}
