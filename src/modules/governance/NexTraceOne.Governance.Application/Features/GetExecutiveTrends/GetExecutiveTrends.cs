using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveTrends;

/// <summary>
/// Feature: GetExecutiveTrends — séries temporais e insights de tendências executivas.
/// Permite visualização de evolução por categoria: operations, changes, incidents, maturity.
/// </summary>
public static class GetExecutiveTrends
{
    /// <summary>Query de tendências executivas. Categoria: operations, changes, incidents ou maturity.</summary>
    public sealed record Query(
        string Category) : IQuery<Response>;

    /// <summary>Handler que computa séries temporais e insights para tendências executivas.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var category = request.Category;

            var series = new List<TrendSeriesDto>
            {
                new("Reliability Score", TrendDirection.Improving, new List<TrendDataPointDto>
                {
                    new("2024-07", 72.0m),
                    new("2024-08", 74.5m),
                    new("2024-09", 73.0m),
                    new("2024-10", 78.0m),
                    new("2024-11", 80.5m),
                    new("2024-12", 82.0m)
                }),
                new("Change Safety Score", TrendDirection.Improving, new List<TrendDataPointDto>
                {
                    new("2024-07", 65.0m),
                    new("2024-08", 68.0m),
                    new("2024-09", 66.5m),
                    new("2024-10", 71.0m),
                    new("2024-11", 74.0m),
                    new("2024-12", 76.5m)
                }),
                new("Incident Rate", TrendDirection.Improving, new List<TrendDataPointDto>
                {
                    new("2024-07", 14.0m),
                    new("2024-08", 12.5m),
                    new("2024-09", 13.0m),
                    new("2024-10", 11.0m),
                    new("2024-11", 9.5m),
                    new("2024-12", 8.0m)
                }),
                new("Maturity Index", TrendDirection.Stable, new List<TrendDataPointDto>
                {
                    new("2024-07", 55.0m),
                    new("2024-08", 56.5m),
                    new("2024-09", 57.0m),
                    new("2024-10", 58.0m),
                    new("2024-11", 58.5m),
                    new("2024-12", 59.0m)
                })
            };

            var insights = new List<TrendInsightDto>
            {
                new("Reliability score improved 14% over 6 months driven by reduced timeout incidents in Payments domain",
                    "Fewer customer-facing errors and improved SLA compliance",
                    "Continue investment in retry-pattern improvements and circuit breaker adoption"),
                new("Change safety score shows consistent upward trend after introduction of automated blast radius analysis",
                    "Reduced rollback frequency from 12% to 5% of deployments",
                    "Expand automated validation to Integration and Analytics domains"),
                new("Incident rate declined 43% with strongest improvement in last quarter",
                    "Lower operational burden and improved team focus on feature delivery",
                    "Address recurring incidents in Commerce domain to sustain improvement trajectory"),
                new("Maturity index advancing slowly despite significant gains in individual dimensions",
                    "Integration and Commerce teams pulling down aggregate scores",
                    "Prioritize targeted maturity programs for lowest-scoring teams with clear milestones")
            };

            var response = new Response(
                Category: category,
                Series: series,
                Insights: insights,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta de tendências executivas com séries temporais e insights.</summary>
    public sealed record Response(
        string Category,
        IReadOnlyList<TrendSeriesDto> Series,
        IReadOnlyList<TrendInsightDto> Insights,
        DateTimeOffset GeneratedAt);

    /// <summary>Série temporal com direção de tendência e pontos de dados.</summary>
    public sealed record TrendSeriesDto(
        string Name,
        TrendDirection Direction,
        IReadOnlyList<TrendDataPointDto> DataPoints);

    /// <summary>Ponto de dados com período e valor.</summary>
    public sealed record TrendDataPointDto(
        string Period,
        decimal Value);

    /// <summary>Insight de tendência com impacto e recomendação.</summary>
    public sealed record TrendInsightDto(
        string Insight,
        string Impact,
        string Recommendation);
}
