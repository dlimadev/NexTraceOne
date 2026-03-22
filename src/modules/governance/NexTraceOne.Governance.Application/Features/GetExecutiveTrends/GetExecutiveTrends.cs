using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
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
    public sealed class Handler(IGovernanceAnalyticsRepository analyticsRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var category = request.Category;

            // Get real data from repository for 6 months
            var waiverCounts = await analyticsRepository.GetWaiverCountsByMonthAsync(6, cancellationToken);
            var packCounts = await analyticsRepository.GetPublishedPackCountsByMonthAsync(6, cancellationToken);
            var rolloutCounts = await analyticsRepository.GetRolloutCountsByMonthAsync(6, cancellationToken);

            // Build trend series from real data
            var series = new List<TrendSeriesDto>
            {
                new("Governance Waiver Requests", CalculateTrend(waiverCounts), waiverCounts.Select(w => new TrendDataPointDto(w.Period, w.Count)).ToList()),
                new("Published Governance Packs", CalculateTrend(packCounts), packCounts.Select(p => new TrendDataPointDto(p.Period, p.Count)).ToList()),
                new("Pack Rollouts", CalculateTrend(rolloutCounts), rolloutCounts.Select(r => new TrendDataPointDto(r.Period, r.Count)).ToList())
            };

            // Generate insights based on data
            var insights = GenerateInsights(waiverCounts, packCounts, rolloutCounts);

            var response = new Response(
                Category: category,
                Series: series,
                Insights: insights,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false);

            return Result<Response>.Success(response);
        }

        private static TrendDirection CalculateTrend(IReadOnlyList<MonthlyCount> data)
        {
            if (data.Count < 2) return TrendDirection.Stable;

            var firstHalf = data.Take(data.Count / 2).Average(d => d.Count);
            var secondHalf = data.Skip(data.Count / 2).Average(d => d.Count);

            var changePercent = firstHalf > 0 ? ((secondHalf - firstHalf) / firstHalf) * 100 : 0;

            return changePercent switch
            {
                > 10 => TrendDirection.Improving,
                < -10 => TrendDirection.Declining,
                _ => TrendDirection.Stable
            };
        }

        private static List<TrendInsightDto> GenerateInsights(
            IReadOnlyList<MonthlyCount> waiverCounts,
            IReadOnlyList<MonthlyCount> packCounts,
            IReadOnlyList<MonthlyCount> rolloutCounts)
        {
            var insights = new List<TrendInsightDto>();

            // Waiver insights
            var totalWaivers = waiverCounts.Sum(w => w.Count);
            if (totalWaivers > 0)
            {
                insights.Add(new TrendInsightDto(
                    Insight: $"{totalWaivers} governance waiver requests in the last {waiverCounts.Count} months",
                    Impact: "Indicates areas where standard governance may need refinement",
                    Recommendation: "Review waiver patterns to identify systematic governance gaps"));
            }

            // Pack insights
            var totalPacks = packCounts.Sum(p => p.Count);
            if (totalPacks > 0)
            {
                insights.Add(new TrendInsightDto(
                    Insight: $"{totalPacks} governance packs published in the last {packCounts.Count} months",
                    Impact: "Growing governance coverage across domains",
                    Recommendation: "Ensure pack adoption through rollout tracking and compliance monitoring"));
            }

            // Rollout insights
            var totalRollouts = rolloutCounts.Sum(r => r.Count);
            if (totalRollouts > 0)
            {
                insights.Add(new TrendInsightDto(
                    Insight: $"{totalRollouts} governance pack rollouts executed in the last {rolloutCounts.Count} months",
                    Impact: "Active governance enforcement across scopes",
                    Recommendation: "Monitor rollout success rates and address blocked rollouts"));
            }

            // Fallback insight if no data
            if (insights.Count == 0)
            {
                insights.Add(new TrendInsightDto(
                    Insight: "Limited governance activity data available",
                    Impact: "Unable to assess governance maturity trends",
                    Recommendation: "Activate governance packs and establish baseline metrics"));
            }

            return insights;
        }
    }

    /// <summary>Resposta de tendências executivas com séries temporais e insights.</summary>
    public sealed record Response(
        string Category,
        IReadOnlyList<TrendSeriesDto> Series,
        IReadOnlyList<TrendInsightDto> Insights,
        DateTimeOffset GeneratedAt,
        bool IsSimulated);

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
