using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTeamOperationalHealthReport;

/// <summary>
/// Feature: GetTeamOperationalHealthReport — scorecard de saúde operacional por equipa.
///
/// Agrega métricas de SLO compliance, drift findings, chaos experiments e profiling
/// cobertura por equipa, produzindo um score composto ponderado:
/// - SLO compliance: 40%
/// - Drift (penalidade por findings não reconhecidos): 30%
/// - Chaos success rate: 20%
/// - Profiling coverage: 10%
///
/// Classifica a saúde operacional de cada equipa em:
/// - <c>Excellent</c> — score ≥ 90
/// - <c>Good</c> — score ≥ 70
/// - <c>Fair</c> — score ≥ 50
/// - <c>Poor</c> — score &lt; 50
///
/// Produz:
/// - lista de equipas com scorecard completo e tier de saúde
/// - distribuição por tier
/// - métricas agregadas globais do tenant
/// - top equipas mais saudáveis e mais problemáticas
///
/// Permite que Tech Lead, Architect e Executive comparem a maturidade operacional
/// entre equipas e priorizem intervenções de melhoria de confiabilidade.
///
/// Wave R.3 — Team Operational Health Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetTeamOperationalHealthReport
{
    // ── Pesos do score composto ────────────────────────────────────────────
    private const decimal SloWeight = 0.40m;
    private const decimal DriftWeight = 0.30m;
    private const decimal ChaosWeight = 0.20m;
    private const decimal ProfilingWeight = 0.10m;

    // ── Limiares de tier ──────────────────────────────────────────────────
    private const decimal ExcellentThreshold = 90m;
    private const decimal GoodThreshold = 70m;
    private const decimal FairThreshold = 50m;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–90, default 30).</para>
    /// <para><c>TopTeamsCount</c>: número máximo de equipas nos rankings (1–100, default 20).</para>
    /// <para><c>MaxDriftPenaltyPerService</c>: número de drift findings não reconhecidos por serviço que resulta em penalidade máxima de drift (1–20, default 5).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int TopTeamsCount = 20,
        int MaxDriftPenaltyPerService = 5) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de saúde operacional de uma equipa.</summary>
    public enum OperationalHealthTier
    {
        /// <summary>Score ≥ 90 — equipa com excelente maturidade operacional.</summary>
        Excellent,
        /// <summary>Score ≥ 70 — equipa com boa saúde operacional.</summary>
        Good,
        /// <summary>Score ≥ 50 — equipa com saúde operacional razoável, requer atenção.</summary>
        Fair,
        /// <summary>Score &lt; 50 — equipa com saúde operacional deficiente, requer intervenção.</summary>
        Poor
    }

    /// <summary>Distribuição de equipas por tier de saúde operacional.</summary>
    public sealed record HealthTierDistribution(
        int ExcellentCount,
        int GoodCount,
        int FairCount,
        int PoorCount);

    /// <summary>Scorecard de saúde operacional de uma equipa.</summary>
    public sealed record TeamHealthEntry(
        string TeamName,
        int ServiceCount,
        decimal SloComplianceRatePct,
        int UnacknowledgedDriftCount,
        decimal ChaosSuccessRatePct,
        int ServicesWithProfilingCount,
        int PostDeployIncidentCount,
        decimal CompositeHealthScore,
        OperationalHealthTier HealthTier);

    /// <summary>Resultado do relatório de saúde operacional por equipa.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalTeamsAnalyzed,
        decimal TenantAvgHealthScore,
        HealthTierDistribution TierDistribution,
        IReadOnlyList<TeamHealthEntry> TopHealthyTeams,
        IReadOnlyList<TeamHealthEntry> TopAtRiskTeams,
        IReadOnlyList<TeamHealthEntry> AllTeams);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(q => q.TopTeamsCount).InclusiveBetween(1, 100);
            RuleFor(q => q.MaxDriftPenaltyPerService).InclusiveBetween(1, 20);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ITeamOperationalMetricsReader _metricsReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            ITeamOperationalMetricsReader metricsReader,
            IDateTimeProvider clock)
        {
            _metricsReader = Guard.Against.Null(metricsReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            var teamMetrics = await _metricsReader.ListTeamMetricsAsync(
                query.TenantId, from, now, cancellationToken);

            var entries = teamMetrics
                .Select(m => BuildEntry(m, query.MaxDriftPenaltyPerService))
                .ToList();

            int excellentCount = entries.Count(e => e.HealthTier == OperationalHealthTier.Excellent);
            int goodCount = entries.Count(e => e.HealthTier == OperationalHealthTier.Good);
            int fairCount = entries.Count(e => e.HealthTier == OperationalHealthTier.Fair);
            int poorCount = entries.Count(e => e.HealthTier == OperationalHealthTier.Poor);

            decimal tenantAvg = entries.Count > 0
                ? Math.Round(entries.Average(e => e.CompositeHealthScore), 2)
                : 0m;

            var topHealthy = entries
                .OrderByDescending(e => e.CompositeHealthScore)
                .Take(query.TopTeamsCount)
                .ToList();

            var topAtRisk = entries
                .OrderBy(e => e.CompositeHealthScore)
                .Take(query.TopTeamsCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalTeamsAnalyzed: entries.Count,
                TenantAvgHealthScore: tenantAvg,
                TierDistribution: new HealthTierDistribution(excellentCount, goodCount, fairCount, poorCount),
                TopHealthyTeams: topHealthy,
                TopAtRiskTeams: topAtRisk,
                AllTeams: entries.OrderBy(e => e.TeamName).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static TeamHealthEntry BuildEntry(TeamOperationalMetrics metrics, int maxDriftPenaltyPerService)
        {
            // SLO component: direct percentage (0–100)
            decimal sloScore = Math.Clamp(metrics.SloComplianceRatePct, 0m, 100m);

            // Drift component: penalise for unacknowledged drift findings
            // Maximum penalty when drift per service reaches maxDriftPenaltyPerService
            decimal driftPenaltyPerService = metrics.ServiceCount > 0
                ? (decimal)metrics.UnacknowledgedDriftCount / metrics.ServiceCount
                : 0m;
            decimal driftScore = Math.Clamp(
                100m - (driftPenaltyPerService / maxDriftPenaltyPerService * 100m),
                0m, 100m);

            // Chaos component: direct percentage (0–100)
            decimal chaosScore = Math.Clamp(metrics.ChaosSuccessRatePct, 0m, 100m);

            // Profiling component: percentage of services with profiling
            decimal profilingScore = metrics.ServiceCount > 0
                ? Math.Clamp((decimal)metrics.ServicesWithProfilingCount / metrics.ServiceCount * 100m, 0m, 100m)
                : 0m;

            decimal composite = Math.Round(
                sloScore * SloWeight +
                driftScore * DriftWeight +
                chaosScore * ChaosWeight +
                profilingScore * ProfilingWeight,
                2);

            return new TeamHealthEntry(
                TeamName: metrics.TeamName,
                ServiceCount: metrics.ServiceCount,
                SloComplianceRatePct: Math.Round(metrics.SloComplianceRatePct, 2),
                UnacknowledgedDriftCount: metrics.UnacknowledgedDriftCount,
                ChaosSuccessRatePct: Math.Round(metrics.ChaosSuccessRatePct, 2),
                ServicesWithProfilingCount: metrics.ServicesWithProfilingCount,
                PostDeployIncidentCount: metrics.PostDeployIncidentCount,
                CompositeHealthScore: composite,
                HealthTier: ClassifyTier(composite));
        }

        private static OperationalHealthTier ClassifyTier(decimal score) => score switch
        {
            >= ExcellentThreshold => OperationalHealthTier.Excellent,
            >= GoodThreshold => OperationalHealthTier.Good,
            >= FairThreshold => OperationalHealthTier.Fair,
            _ => OperationalHealthTier.Poor
        };
    }
}
