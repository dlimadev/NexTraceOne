using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSreMaturityIndexReport;

/// <summary>
/// Feature: GetSreMaturityIndexReport — índice de maturidade SRE por equipa.
///
/// Avalia 6 práticas SRE com base em evidências observadas no NexTraceOne:
/// - <c>SloDefinitionCoverage</c>     (20%) — % serviços com SLO e SloObservation activa
/// - <c>ErrorBudgetTracking</c>       (20%) — % serviços com ErrorBudgetTier calculado
/// - <c>ChaosEngineeringAdoption</c>  (15%) — % serviços com ChaosExperiment no último trimestre
/// - <c>ToilReductionEvidence</c>     (15%) — AutoApproval ou pipeline automation
/// - <c>PostIncidentReviewRate</c>    (15%) — % incidentes Severe/Critical com PostIncidentLearning
/// - <c>RunbookCompleteness</c>       (15%) — % incidentes com runbook activo no serviço
///
/// <c>SreMaturityTier</c>: Elite ≥85 / Advanced ≥65 / Practicing ≥40 / Foundational &lt;40
///
/// Wave AN.3 — SRE Intelligence &amp; Error Budget Management (OperationalIntelligence Runtime).
/// </summary>
public static class GetSreMaturityIndexReport
{
    // ── Dimension weights ──────────────────────────────────────────────────
    private const decimal SloDefinitionWeight = 0.20m;
    private const decimal ErrorBudgetWeight = 0.20m;
    private const decimal ChaosWeight = 0.15m;
    private const decimal ToilReductionWeight = 0.15m;
    private const decimal PostIncidentWeight = 0.15m;
    private const decimal RunbookWeight = 0.15m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal EliteThreshold = 85m;
    private const decimal AdvancedThreshold = 65m;
    private const decimal PracticingThreshold = 40m;

    internal const int DefaultChaosLookbackMonths = 3;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int ChaosLookbackMonths = DefaultChaosLookbackMonths) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ChaosLookbackMonths).InclusiveBetween(1, 12);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum SreMaturityTier { Elite, Advanced, Practicing, Foundational }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record SreDimensionScores(
        decimal SloDefinitionScore,
        decimal ErrorBudgetScore,
        decimal ChaosEngineeringScore,
        decimal ToilReductionScore,
        decimal PostIncidentReviewScore,
        decimal RunbookCompletenessScore);

    public sealed record WeakestPractice(string DimensionName, decimal Score, string ImprovementSuggestion);

    public sealed record TeamSreMaturityRow(
        string TeamId,
        string TeamName,
        int TotalServices,
        decimal SreMaturityScore,
        SreMaturityTier Tier,
        SreDimensionScores DimensionScores,
        IReadOnlyList<WeakestPractice> WeakestPractices,
        decimal? TrendVsPreviousPeriod);

    public sealed record Report(
        string TenantId,
        int TotalTeamsAnalyzed,
        int EliteTeamCount,
        int AdvancedTeamCount,
        int PracticingTeamCount,
        int FoundationalTeamCount,
        decimal TenantSreMaturityIndex,
        SreMaturityTier TenantTier,
        IReadOnlyList<TeamSreMaturityRow> ByTeam,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISreMaturityReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListByTenantAsync(request.TenantId, request.ChaosLookbackMonths, cancellationToken);

            var rows = entries.Select(e =>
            {
                var totalSvc = Math.Max(1, e.TotalServices);

                // 1. SLO Definition Coverage
                var sloScore = Math.Min((decimal)e.ServicesWithSlo / totalSvc * 100m, 100m);

                // 2. Error Budget Tracking
                var ebScore = Math.Min((decimal)e.ServicesWithErrorBudgetTracking / totalSvc * 100m, 100m);

                // 3. Chaos Engineering Adoption
                var chaosScore = Math.Min((decimal)e.ServicesWithChaosExperiment / totalSvc * 100m, 100m);

                // 4. Toil Reduction Evidence (binary)
                var toilScore = e.HasAutoApprovalOrPipelineAutomation ? 100m : 0m;

                // 5. Post-Incident Review Rate
                var pirScore = e.TotalSevereOrCriticalIncidents == 0 ? 100m
                    : Math.Min((decimal)e.IncidentsWithPostIncidentReview / e.TotalSevereOrCriticalIncidents * 100m, 100m);

                // 6. Runbook Completeness
                var rkScore = e.TotalIncidentsWithService == 0 ? 100m
                    : Math.Min((decimal)e.IncidentsWithActiveRunbook / e.TotalIncidentsWithService * 100m, 100m);

                var compositeScore = Math.Round(
                    sloScore * SloDefinitionWeight +
                    ebScore * ErrorBudgetWeight +
                    chaosScore * ChaosWeight +
                    toilScore * ToilReductionWeight +
                    pirScore * PostIncidentWeight +
                    rkScore * RunbookWeight, 2);

                var tier = compositeScore >= EliteThreshold ? SreMaturityTier.Elite
                    : compositeScore >= AdvancedThreshold ? SreMaturityTier.Advanced
                    : compositeScore >= PracticingThreshold ? SreMaturityTier.Practicing
                    : SreMaturityTier.Foundational;

                var dimScores = new SreDimensionScores(
                    Math.Round(sloScore, 2), Math.Round(ebScore, 2),
                    Math.Round(chaosScore, 2), Math.Round(toilScore, 2),
                    Math.Round(pirScore, 2), Math.Round(rkScore, 2));

                // Weakest 2 practices
                var dims = new[]
                {
                    ("SLO Definition Coverage", dimScores.SloDefinitionScore, "Ensure all services have active SLO observations"),
                    ("Error Budget Tracking", dimScores.ErrorBudgetScore, "Enable error budget calculation for all services"),
                    ("Chaos Engineering Adoption", dimScores.ChaosEngineeringScore, "Run chaos experiments for critical services"),
                    ("Toil Reduction Evidence", dimScores.ToilReductionScore, "Enable auto-approval or pipeline automation"),
                    ("Post-Incident Review Rate", dimScores.PostIncidentReviewScore, "Document post-incident learnings for Severe/Critical incidents"),
                    ("Runbook Completeness", dimScores.RunbookCompletenessScore, "Link active runbooks to all services with incidents")
                };

                var weakest = dims.OrderBy(d => d.Item2).Take(2)
                    .Select(d => new WeakestPractice(d.Item1, d.Item2, d.Item3))
                    .ToList();

                var trend = e.PreviousPeriodSreScore.HasValue
                    ? Math.Round(compositeScore - e.PreviousPeriodSreScore.Value, 2)
                    : (decimal?)null;

                return new TeamSreMaturityRow(
                    e.TeamId, e.TeamName, e.TotalServices,
                    compositeScore, tier, dimScores, weakest, trend);
            }).ToList();

            // Tenant SRE Maturity Index weighted by team service count
            var weightedSum = rows.Sum(r => r.SreMaturityScore * r.TotalServices);
            var totalServices = rows.Sum(r => r.TotalServices);
            var tenantIndex = rows.Count == 0 ? 100m
                : Math.Round(totalServices == 0 ? rows.Average(r => r.SreMaturityScore) : weightedSum / totalServices, 2);

            var tenantTier = tenantIndex >= EliteThreshold ? SreMaturityTier.Elite
                : tenantIndex >= AdvancedThreshold ? SreMaturityTier.Advanced
                : tenantIndex >= PracticingThreshold ? SreMaturityTier.Practicing
                : SreMaturityTier.Foundational;

            return Result<Report>.Success(new Report(
                request.TenantId,
                rows.Count,
                rows.Count(r => r.Tier == SreMaturityTier.Elite),
                rows.Count(r => r.Tier == SreMaturityTier.Advanced),
                rows.Count(r => r.Tier == SreMaturityTier.Practicing),
                rows.Count(r => r.Tier == SreMaturityTier.Foundational),
                tenantIndex, tenantTier, rows, now));
        }
    }
}
