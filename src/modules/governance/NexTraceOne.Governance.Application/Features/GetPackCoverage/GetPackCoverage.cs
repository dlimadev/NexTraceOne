using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetPackCoverage;

/// <summary>
/// Feature: GetPackCoverage — cobertura de conformidade de um governance pack.
/// Retorna métricas de cobertura por escopo com percentagem de conformidade.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class GetPackCoverage
{
    /// <summary>Query para obter a cobertura de conformidade de um governance pack.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>Handler que retorna as métricas de cobertura do governance pack.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // TODO [P03.5]: Replace static coverage computation with rollout/rule-evaluation engine integration
            // when per-scope compliance evaluation contract becomes available.
            var items = new List<CoverageItemDto>
            {
                new("Domain", "payments", 18, 16, 2, 88.9m),
                new("Domain", "operations", 18, 14, 4, 77.8m),
                new("Team", "platform-core", 18, 17, 1, 94.4m),
                new("Team", "growth", 18, 12, 6, 66.7m),
                new("Environment", "Production", 18, 15, 3, 83.3m)
            };

            var totalRules = items.Sum(i => i.TotalRules);
            var totalCompliant = items.Sum(i => i.CompliantCount);
            var overallPercent = totalRules > 0
                ? Math.Round((decimal)totalCompliant / totalRules * 100, 1)
                : 0m;

            var response = new Response(
                PackId: request.PackId,
                OverallCoveragePercent: overallPercent,
                TotalScopes: items.Count,
                Items: items);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com métricas de cobertura do governance pack.</summary>
    public sealed record Response(
        string PackId,
        decimal OverallCoveragePercent,
        int TotalScopes,
        IReadOnlyList<CoverageItemDto> Items);

    /// <summary>DTO de cobertura de conformidade por escopo.</summary>
    public sealed record CoverageItemDto(
        string ScopeType,
        string ScopeValue,
        int TotalRules,
        int CompliantCount,
        int NonCompliantCount,
        decimal CoveragePercent);
}
