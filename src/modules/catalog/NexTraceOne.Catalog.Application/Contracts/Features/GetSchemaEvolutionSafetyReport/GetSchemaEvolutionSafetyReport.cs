using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaEvolutionSafetyReport;

/// <summary>
/// Feature: GetSchemaEvolutionSafetyReport — análise de segurança de evolução de schema.
/// Wave AQ.3 — Schema Evolution Safety.
/// </summary>
public static class GetSchemaEvolutionSafetyReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        double BreakingChangeLowPct = 5.0,
        double BreakingChangeDangerousPct = 25.0) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.BreakingChangeLowPct).InclusiveBetween(0.0, 100.0);
            RuleFor(x => x.BreakingChangeDangerousPct).InclusiveBetween(0.0, 100.0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum EvolutionSafetyTier { Safe, Cautious, Risky, Dangerous }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record TeamEvolutionRow(
        string TeamId,
        string TeamName,
        int TotalSchemaChanges,
        int BreakingChanges,
        double BreakingChangeRate,
        double SafeEvolutionRate,
        double ConsumerNotificationRate,
        EvolutionSafetyTier Tier,
        IReadOnlyList<ISchemaEvolutionSafetyReader.ProtocolBreakingEntry> ProtocolBreaking,
        IReadOnlyList<ISchemaEvolutionSafetyReader.HighRiskChange> RecentHighRiskChanges);

    public sealed record ProtocolBreakingRateRow(string Protocol, int TotalChanges, int TotalBreaking, double BreakingRate);

    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int TeamsAnalyzed,
        double TenantEvolutionSafetyIndex,
        IReadOnlyList<ProtocolBreakingRateRow> ProtocolBreakingRateComparison,
        IReadOnlyList<ISchemaEvolutionSafetyReader.HighRiskChange> HighRiskSchemaChanges,
        IReadOnlyList<string> EvolutionPatternRecommendations,
        IReadOnlyList<TeamEvolutionRow> TeamDetails);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISchemaEvolutionSafetyReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListByTenantAsync(request.TenantId, request.LookbackDays, cancellationToken);

            var teamRows = entries.Select(e => BuildTeamRow(e, request.BreakingChangeLowPct, request.BreakingChangeDangerousPct)).ToList();

            int nonDangerous = teamRows.Count(r => r.Tier != EvolutionSafetyTier.Dangerous);
            double safetyIndex = teamRows.Count == 0 ? 100.0
                : Math.Round((double)nonDangerous / teamRows.Count * 100.0, 2);

            var protocolComparison = BuildProtocolComparison(entries);

            var highRiskChanges = teamRows
                .Where(r => r.Tier == EvolutionSafetyTier.Risky || r.Tier == EvolutionSafetyTier.Dangerous)
                .SelectMany(r => r.RecentHighRiskChanges)
                .ToList();

            var recommendations = BuildRecommendations(teamRows, request.BreakingChangeDangerousPct);

            var report = new Report(now, request.TenantId, request.LookbackDays,
                teamRows.Count, safetyIndex, protocolComparison,
                highRiskChanges, recommendations, teamRows);

            return Result<Report>.Success(report);
        }

        private static TeamEvolutionRow BuildTeamRow(
            ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry e,
            double lowPct, double dangerousPct)
        {
            double breakingRate = Math.Round((double)e.BreakingChanges / Math.Max(e.TotalSchemaChanges, 1) * 100.0, 2);
            double safeRate = Math.Round(100.0 - breakingRate, 2);
            double notificationRate = e.BreakingChanges == 0 ? 100.0
                : Math.Round((double)e.ConsumerNotifiedBreakingChanges / e.BreakingChanges * 100.0, 2);

            EvolutionSafetyTier tier;
            if (breakingRate > dangerousPct || e.BreakingChangesWithIncidentCorrelation > 0)
                tier = EvolutionSafetyTier.Dangerous;
            else if (breakingRate <= lowPct && notificationRate >= 90.0)
                tier = EvolutionSafetyTier.Safe;
            else if (breakingRate > lowPct * 2)
                tier = EvolutionSafetyTier.Risky;
            else
                tier = EvolutionSafetyTier.Cautious;

            return new TeamEvolutionRow(
                e.TeamId, e.TeamName, e.TotalSchemaChanges, e.BreakingChanges,
                breakingRate, safeRate, notificationRate, tier,
                e.ProtocolBreaking, e.RecentHighRiskChanges);
        }

        private static IReadOnlyList<ProtocolBreakingRateRow> BuildProtocolComparison(
            IReadOnlyList<ISchemaEvolutionSafetyReader.TeamSchemaEvolutionEntry> entries)
        {
            return entries
                .SelectMany(e => e.ProtocolBreaking)
                .GroupBy(p => p.Protocol)
                .Select(g =>
                {
                    int totalAll = g.Sum(p => p.Total);
                    int totalBreaking = g.Sum(p => p.Breaking);
                    double rate = Math.Round((double)totalBreaking / Math.Max(totalAll, 1) * 100.0, 2);
                    return new ProtocolBreakingRateRow(g.Key, totalAll, totalBreaking, rate);
                })
                .ToList();
        }

        private static IReadOnlyList<string> BuildRecommendations(
            IReadOnlyList<TeamEvolutionRow> rows, double dangerousPct)
        {
            var recommendations = new List<string>();
            foreach (var row in rows.Where(r => r.Tier == EvolutionSafetyTier.Risky || r.Tier == EvolutionSafetyTier.Dangerous))
            {
                if (row.BreakingChangeRate > dangerousPct)
                    recommendations.Add($"Team {row.TeamName}: adopt API versioning strategy to reduce breaking change rate");
                if (row.ConsumerNotificationRate < 50.0)
                    recommendations.Add($"Team {row.TeamName}: improve consumer notification for breaking schema changes");
            }
            return recommendations.Distinct().ToList();
        }
    }
}
