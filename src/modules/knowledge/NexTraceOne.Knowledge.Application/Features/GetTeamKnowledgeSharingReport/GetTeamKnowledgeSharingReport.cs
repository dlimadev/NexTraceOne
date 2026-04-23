using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application.Features.GetTeamKnowledgeSharingReport;

/// <summary>
/// Feature: GetTeamKnowledgeSharingReport — análise de partilha de conhecimento entre equipas.
///
/// Mede a saúde do sharing de conhecimento cross-team:
/// - <c>KnowledgeSharingRatio</c> = CrossTeamContributions / TotalContributions por equipa
/// - <c>KnowledgeSiloRisk</c> = true quando KnowledgeSharingRatio &lt; silo_threshold
/// - <c>TenantKnowledgeSharingScore</c> = % equipas sem silo risk
/// - <c>BusFactor1Services</c> = serviços com contribuidores únicos de documentação
/// - <c>CollaborationTrend</c> (90d) via WeeklyKnowledgeSharingSnapshots
///
/// Wave AY.3 — Organizational Knowledge &amp; Documentation Intelligence.
/// </summary>
public static class GetTeamKnowledgeSharingReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        decimal SiloThreshold = 0.15m,
        int BusFactorMaxContributors = 1,
        int TopContributorsCount = 5) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 365);
            RuleFor(x => x.SiloThreshold).InclusiveBetween(0m, 1m);
            RuleFor(x => x.BusFactorMaxContributors).InclusiveBetween(1, 10);
            RuleFor(x => x.TopContributorsCount).InclusiveBetween(1, 20);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum CollaborationTrendDirection { Growing, Stable, Declining }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record TeamKnowledgeSharingSummary(
        string TeamId,
        string TeamName,
        int DocContributionCount,
        int CrossTeamContributions,
        int DocConsumptionCount,
        int RunbookContributionCount,
        decimal KnowledgeSharingRatio,
        bool KnowledgeSiloRisk);

    public sealed record KnowledgeFlowEdge(
        string SourceTeamId,
        string SourceTeamName,
        string TargetTeamId,
        string TargetTeamName,
        int ContributionCount);

    public sealed record WeeklyTrendPoint(
        int WeekOffset,
        decimal KnowledgeSharingRatio);

    public sealed record TenantKnowledgeSharingSummary(
        IReadOnlyList<string> TeamsWithSiloRisk,
        IReadOnlyList<TeamKnowledgeSharingSummary> TopKnowledgeContributors,
        decimal TenantKnowledgeSharingScore,
        IReadOnlyList<KnowledgeFlowEdge> KnowledgeFlowGraph);

    public sealed record Report(
        string TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        IReadOnlyList<TeamKnowledgeSharingSummary> ByTeam,
        TenantKnowledgeSharingSummary Summary,
        IReadOnlyList<string> KnowledgeHotspots,
        IReadOnlyList<string> KnowledgeColdSpots,
        IReadOnlyList<WeeklyTrendPoint> CollaborationTrend,
        CollaborationTrendDirection TrendDirection,
        IReadOnlyList<string> BusFactor1Services);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ITeamKnowledgeSharingReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);

            var teamEntries = await reader.ListByTenantAsync(request.TenantId, from, now, cancellationToken);
            var trendSnapshots = await reader.GetTenantTrendAsync(request.TenantId, from, now, cancellationToken);
            var serviceContribs = await reader.ListServiceContributionsAsync(request.TenantId, from, now, cancellationToken);

            var byTeam = teamEntries.Select(e => BuildTeamEntry(e, request.SiloThreshold)).ToList();

            var busFactor1 = serviceContribs
                .Where(s => s.ContributorIds.Distinct().Count() <= request.BusFactorMaxContributors)
                .Select(s => s.ServiceName)
                .ToList();

            var hotspots = serviceContribs
                .OrderByDescending(s => s.ContributorIds.Distinct().Count())
                .Take(5)
                .Where(s => s.ContributorIds.Distinct().Count() > 1)
                .Select(s => s.ServiceName)
                .ToList();

            var coldSpots = busFactor1.Take(5).ToList();

            var trendPoints = trendSnapshots
                .Select(s => new WeeklyTrendPoint(s.WeekOffset, s.KnowledgeSharingRatio))
                .ToList();

            var trendDir = ComputeTrendDirection(trendPoints);

            var summary = BuildSummary(byTeam, teamEntries, request);

            return Result<Report>.Success(new Report(
                request.TenantId, from, now, now, request.LookbackDays,
                byTeam, summary,
                hotspots, coldSpots, trendPoints, trendDir, busFactor1));
        }

        private static TeamKnowledgeSharingSummary BuildTeamEntry(
            ITeamKnowledgeSharingReader.TeamKnowledgeEntry e,
            decimal siloThreshold)
        {
            var total = e.DocContributionCount;
            var ratio = total > 0
                ? Math.Round((decimal)e.CrossTeamContributions / total, 3)
                : 0m;
            var siloRisk = ratio < siloThreshold;

            return new TeamKnowledgeSharingSummary(
                e.TeamId, e.TeamName,
                e.DocContributionCount, e.CrossTeamContributions, e.DocConsumptionCount,
                e.RunbookContributionCount, ratio, siloRisk);
        }

        private static TenantKnowledgeSharingSummary BuildSummary(
            IReadOnlyList<TeamKnowledgeSharingSummary> byTeam,
            IReadOnlyList<ITeamKnowledgeSharingReader.TeamKnowledgeEntry> rawEntries,
            Query request)
        {
            var siloTeams = byTeam
                .Where(t => t.KnowledgeSiloRisk)
                .Select(t => t.TeamName)
                .ToList();

            var topContributors = byTeam
                .OrderByDescending(t => t.CrossTeamContributions)
                .Take(request.TopContributorsCount)
                .ToList();

            var score = byTeam.Count > 0
                ? Math.Round((decimal)byTeam.Count(t => !t.KnowledgeSiloRisk) / byTeam.Count * 100, 1)
                : 100m;

            var flowEdges = BuildFlowGraph(rawEntries, byTeam);

            return new TenantKnowledgeSharingSummary(siloTeams, topContributors, score, flowEdges);
        }

        private static IReadOnlyList<KnowledgeFlowEdge> BuildFlowGraph(
            IReadOnlyList<ITeamKnowledgeSharingReader.TeamKnowledgeEntry> rawEntries,
            IReadOnlyList<TeamKnowledgeSharingSummary> byTeam)
        {
            var teamNameById = byTeam.ToDictionary(t => t.TeamId, t => t.TeamName);

            return rawEntries
                .Where(e => e.CrossTeamContributions > 0 && e.TargetTeamIds.Count > 0)
                .SelectMany(e => e.TargetTeamIds.Select(targetId => new KnowledgeFlowEdge(
                    e.TeamId, e.TeamName,
                    targetId,
                    teamNameById.TryGetValue(targetId, out var name) ? name : targetId,
                    e.CrossTeamContributions)))
                .Take(20)
                .ToList();
        }

        internal static CollaborationTrendDirection ComputeTrendDirection(
            IReadOnlyList<WeeklyTrendPoint> trendPoints)
        {
            if (trendPoints.Count < 2) return CollaborationTrendDirection.Stable;

            var first = trendPoints[0].KnowledgeSharingRatio;
            var last = trendPoints[trendPoints.Count - 1].KnowledgeSharingRatio;
            var delta = last - first;

            return delta > 0.02m
                ? CollaborationTrendDirection.Growing
                : delta < -0.02m
                    ? CollaborationTrendDirection.Declining
                    : CollaborationTrendDirection.Stable;
        }
    }
}
