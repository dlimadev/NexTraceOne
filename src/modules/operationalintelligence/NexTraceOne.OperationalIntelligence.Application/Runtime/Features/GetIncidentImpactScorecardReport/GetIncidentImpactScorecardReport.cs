using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetIncidentImpactScorecardReport;

/// <summary>
/// Feature: GetIncidentImpactScorecardReport — scorecard composto de impacto de incidentes por serviço e equipa.
///
/// Calcula <c>IncidentImpactScore</c> (0–100) por 4 dimensões ponderadas:
/// - Duration (30%)        — duração em minutos normalizada por threshold
/// - BlastRadius (25%)     — dependentes impactados normalizado
/// - SloImpact (25%)       — percentagem de budget SLO consumido
/// - CustomerFacing (20%)  — flag binário (100 se customer-facing, 0 caso contrário)
///
/// <c>ImpactTier</c>: Minor ≤25 / Moderate ≤55 / Severe ≤80 / Critical >80
///
/// <c>TeamReliabilityTier</c>: Excellent (avg ≤25 + Severe/Critical ≤1/mês) / Good / AtRisk / Struggling
///
/// Wave AN.2 — SRE Intelligence &amp; Error Budget Management (OperationalIntelligence Runtime).
/// </summary>
public static class GetIncidentImpactScorecardReport
{
    // ── Score weights ──────────────────────────────────────────────────────
    private const decimal DurationWeight = 0.30m;
    private const decimal BlastRadiusWeight = 0.25m;
    private const decimal SloImpactWeight = 0.25m;
    private const decimal CustomerFacingWeight = 0.20m;

    // ── Normalization thresholds ───────────────────────────────────────────
    private const decimal MaxDurationMinutes = 480m; // 8 hours
    private const decimal MaxBlastRadius = 20m;
    private const decimal MaxSloImpactPct = 100m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal CriticalThreshold = 80m;
    private const decimal SevereThreshold = 55m;
    private const decimal ModerateThreshold = 25m;

    internal const int DefaultLookbackDays = 30;
    internal const int DefaultRepeatIncidentThreshold = 3;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int RepeatIncidentThreshold = DefaultRepeatIncidentThreshold) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.RepeatIncidentThreshold).InclusiveBetween(1, 20);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum ImpactTier { Minor, Moderate, Severe, Critical }
    public enum TeamReliabilityTier { Excellent, Good, AtRisk, Struggling }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record IncidentImpactRow(
        string IncidentId,
        string ServiceId,
        string ServiceName,
        string TeamName,
        int DurationMinutes,
        int BlastRadiusDependents,
        decimal SloImpactPct,
        bool CustomerFacing,
        decimal IncidentImpactScore,
        ImpactTier Tier,
        DateTimeOffset OccurredAt);

    public sealed record TeamIncidentScorecard(
        string TeamId,
        string TeamName,
        int TotalIncidents,
        decimal IncidentsPerWeek,
        decimal AverageImpactScore,
        decimal MaxImpactScore,
        int SevereOrCriticalCount,
        TeamReliabilityTier ReliabilityTier,
        decimal TrendVsPreviousPeriod);

    public sealed record RepeatOffenderService(
        string ServiceId,
        string ServiceName,
        string TeamName,
        int IncidentCount);

    public sealed record Report(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int TotalIncidentsAnalyzed,
        decimal TenantIncidentHealthIndex,
        IReadOnlyList<TeamIncidentScorecard> ByTeam,
        IReadOnlyList<IncidentImpactRow> TopImpactfulIncidents,
        IReadOnlyList<RepeatOffenderService> RepeatOffenderServices,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IIncidentImpactScorecardReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.LookbackDays);

            var incidents = await reader.ListByTenantAsync(request.TenantId, request.LookbackDays, cancellationToken);

            // Score each incident
            var scored = incidents.Select(e =>
            {
                var durationScore = Math.Min((decimal)e.DurationMinutes / MaxDurationMinutes * 100m, 100m);
                var blastScore = Math.Min((decimal)e.BlastRadiusDependents / MaxBlastRadius * 100m, 100m);
                var sloScore = Math.Min(e.SloImpactPct / MaxSloImpactPct * 100m, 100m);
                var cfScore = e.CustomerFacing ? 100m : 0m;

                var impactScore = Math.Round(
                    durationScore * DurationWeight +
                    blastScore * BlastRadiusWeight +
                    sloScore * SloImpactWeight +
                    cfScore * CustomerFacingWeight, 2);

                var tier = impactScore > CriticalThreshold ? ImpactTier.Critical
                    : impactScore > SevereThreshold ? ImpactTier.Severe
                    : impactScore > ModerateThreshold ? ImpactTier.Moderate
                    : ImpactTier.Minor;

                return new IncidentImpactRow(
                    e.IncidentId, e.ServiceId, e.ServiceName, e.TeamName,
                    e.DurationMinutes, e.BlastRadiusDependents, e.SloImpactPct, e.CustomerFacing,
                    impactScore, tier, e.OccurredAt);
            }).ToList();

            // Build team scorecards
            var weeks = Math.Max(1, request.LookbackDays / 7.0m);
            var byTeam = scored
                .GroupBy(i => new { i.TeamName })
                .Select(g =>
                {
                    var teamIncidents = g.ToList();
                    var avg = Math.Round(teamIncidents.Average(i => i.IncidentImpactScore), 2);
                    var max = teamIncidents.Max(i => i.IncidentImpactScore);
                    var sevCrit = teamIncidents.Count(i => i.Tier is ImpactTier.Severe or ImpactTier.Critical);
                    var perWeek = Math.Round(teamIncidents.Count / weeks, 2);

                    // Excellent: avg ≤ 25 and severe/critical ≤ 1 per 30-day window
                    var sevCritPerMonth = sevCrit / (request.LookbackDays / 30.0m);
                    var reliabilityTier = avg <= 25m && sevCritPerMonth <= 1m ? TeamReliabilityTier.Excellent
                        : avg <= 55m ? TeamReliabilityTier.Good
                        : avg <= 80m ? TeamReliabilityTier.AtRisk
                        : TeamReliabilityTier.Struggling;

                    // TeamId from first entry (simplified — use team name as ID for null impl)
                    var teamId = incidents.FirstOrDefault(e => e.TeamName == g.Key.TeamName)?.TeamId ?? g.Key.TeamName;

                    return new TeamIncidentScorecard(
                        teamId, g.Key.TeamName, teamIncidents.Count, perWeek,
                        avg, max, sevCrit, reliabilityTier, 0m); // TrendVsPreviousPeriod = 0 (no prior data in null impl)
                }).ToList();

            var goodOrBetter = byTeam.Count(t => t.ReliabilityTier is TeamReliabilityTier.Excellent or TeamReliabilityTier.Good);
            var healthIndex = byTeam.Count == 0 ? 100m
                : Math.Round((decimal)goodOrBetter / byTeam.Count * 100m, 2);

            var top10 = scored.OrderByDescending(i => i.IncidentImpactScore).Take(10).ToList();

            // Repeat offenders: services with >= threshold incidents
            var repeatOffenders = scored
                .GroupBy(i => new { i.ServiceId, i.ServiceName, i.TeamName })
                .Where(g => g.Count() >= request.RepeatIncidentThreshold)
                .Select(g => new RepeatOffenderService(g.Key.ServiceId, g.Key.ServiceName, g.Key.TeamName, g.Count()))
                .OrderByDescending(r => r.IncidentCount)
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, since, now,
                scored.Count, healthIndex, byTeam, top10, repeatOffenders, now));
        }
    }
}
