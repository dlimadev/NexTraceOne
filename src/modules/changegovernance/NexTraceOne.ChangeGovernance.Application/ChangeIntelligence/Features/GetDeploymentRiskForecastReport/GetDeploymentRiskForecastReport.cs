using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentRiskForecastReport;

/// <summary>
/// Feature: GetDeploymentRiskForecastReport — scoring preditivo de risco para uma release específica.
///
/// Calcula <c>ForecastRiskScore</c> (0–100) por 5 dimensões ponderadas:
/// - <c>HistoricalRollbackRate</c>   (25%) — padrão histórico de rollbacks do serviço
/// - <c>EnvironmentInstability</c>   (20%) — score de instabilidade do ambiente de destino
/// - <c>ServiceRiskProfileScore</c>  (20%) — score do Risk Center do serviço
/// - <c>ChangeConfidenceInverse</c>  (20%) — 100 - confidence score da release
/// - <c>RecentIncidentRate</c>       (15%) — taxa de deploys falhados/revertidos nos últimos 30 dias
///
/// Classifica por <c>RiskForecastTier</c>:
/// - <c>Low</c>      — score &lt; 25
/// - <c>Moderate</c> — score &lt; 50
/// - <c>High</c>     — score &lt; 75
/// - <c>Critical</c> — score ≥ 75 (aprovação adicional ou delay recomendados)
///
/// Inclui:
/// - <c>ForecastExplanation</c>           — top 3 factores contribuintes
/// - <c>RecommendedActions</c>            — sugestões baseadas nos factores dominantes
/// - <c>TopPendingHighRiskReleases</c>    — top releases pendentes com risco &gt; Moderate no tenant
///
/// Endpoint: <c>GET /api/v1/changes/releases/{id}/risk-forecast</c>
///
/// Wave AI.1 — Deployment Risk Forecast Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetDeploymentRiskForecastReport
{
    // ── Dimension weights ──────────────────────────────────────────────────
    private const decimal HistoricalRollbackWeight = 0.25m;
    private const decimal EnvironmentInstabilityWeight = 0.20m;
    private const decimal ServiceRiskProfileWeight = 0.20m;
    private const decimal ChangeConfidenceInverseWeight = 0.20m;
    private const decimal RecentIncidentRateWeight = 0.15m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    internal const decimal CriticalThreshold = 75m;
    internal const decimal HighThreshold = 50m;
    internal const decimal ModerateThreshold = 25m;

    private const int RecentIncidentLookbackDays = 30;
    private const int TopPendingReleasesDefault = 10;

    /// <summary>
    /// <para><c>ReleaseId</c>: ID da release a analisar (obrigatório).</para>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>MaxTopPendingReleases</c>: número máximo de releases pendentes de alto risco (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        Guid ReleaseId,
        Guid TenantId,
        int MaxTopPendingReleases = TopPendingReleasesDefault) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Nível de risco previsto para a release antes do deployment.</summary>
    public enum RiskForecastTier
    {
        /// <summary>Score &lt; 25 — risco baixo, deployment seguro.</summary>
        Low,
        /// <summary>Score &lt; 50 — risco moderado, monitorar de perto.</summary>
        Moderate,
        /// <summary>Score &lt; 75 — risco alto, revisão e aprovação recomendadas.</summary>
        High,
        /// <summary>Score ≥ 75 — risco crítico, aprovação adicional ou delay recomendados.</summary>
        Critical
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Contribuição de uma dimensão para o ForecastRiskScore.</summary>
    public sealed record DimensionContribution(
        string DimensionName,
        decimal RawScore,
        decimal WeightedScore,
        string Explanation);

    /// <summary>Entrada de release pendente de alto risco no tenant.</summary>
    public sealed record PendingHighRiskRelease(
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string Version,
        decimal ForecastRiskScore,
        RiskForecastTier Tier);

    /// <summary>Resultado do relatório de previsão de risco de deployment.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        Guid ReleaseId,
        string ServiceName,
        string Environment,
        string Version,
        DeploymentStatus CurrentStatus,
        decimal ForecastRiskScore,
        RiskForecastTier Tier,
        IReadOnlyList<DimensionContribution> Dimensions,
        IReadOnlyList<string> ForecastExplanation,
        IReadOnlyList<string> RecommendedActions,
        IReadOnlyList<PendingHighRiskRelease> TopPendingHighRiskReleases);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.ReleaseId).NotEmpty();
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.MaxTopPendingReleases).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IChangeConfidenceBreakdownRepository _confidenceRepo;
        private readonly IServiceRiskProfileRepository _riskProfileRepo;
        private readonly IEnvironmentInstabilityReader _envInstabilityReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IChangeConfidenceBreakdownRepository confidenceRepo,
            IServiceRiskProfileRepository riskProfileRepo,
            IEnvironmentInstabilityReader envInstabilityReader,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _confidenceRepo = Guard.Against.Null(confidenceRepo);
            _riskProfileRepo = Guard.Against.Null(riskProfileRepo);
            _envInstabilityReader = Guard.Against.Null(envInstabilityReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Default(query.ReleaseId);
            Guard.Against.Default(query.TenantId);

            var now = _clock.UtcNow;
            var releaseId = new ReleaseId(query.ReleaseId);
            var tenantId = query.TenantId.ToString();

            // 1. Load target release
            var release = await _releaseRepo.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(query.ReleaseId.ToString());

            // 2. Load tenant risk profiles once (reused for target and top-pending)
            var riskProfiles = await _riskProfileRepo.ListByTenantRankedAsync(tenantId, maxResults: 200, cancellationToken);

            // 3. Compute dimensions for the target release
            var dims = await BuildDimensionsAsync(
                release, tenantId, riskProfiles, now, cancellationToken);

            decimal forecastScore = Math.Round(dims.Sum(d => d.WeightedScore), 1);
            forecastScore = Math.Clamp(forecastScore, 0m, 100m);
            var tier = ClassifyTier(forecastScore);
            var (explanation, actions) = BuildExplanationAndActions(dims, tier);

            // 4. Build top pending high-risk releases in tenant
            var from = now.AddDays(-RecentIncidentLookbackDays);
            var recentReleases = await _releaseRepo.ListInRangeAsync(
                from, now, environment: null, query.TenantId, cancellationToken);

            var topPending = new List<PendingHighRiskRelease>();
            var pendingReleases = recentReleases
                .Where(r => r.Id != releaseId
                    && r.Status is DeploymentStatus.Pending or DeploymentStatus.Running)
                .ToList();

            foreach (var pending in pendingReleases)
            {
                var pendingDims = await BuildDimensionsAsync(
                    pending, tenantId, riskProfiles, now, cancellationToken);
                decimal pendingScore = Math.Round(
                    Math.Clamp(pendingDims.Sum(d => d.WeightedScore), 0m, 100m), 1);

                if (pendingScore >= ModerateThreshold)
                {
                    topPending.Add(new PendingHighRiskRelease(
                        ReleaseId: pending.Id.Value,
                        ServiceName: pending.ServiceName,
                        Environment: pending.Environment,
                        Version: pending.Version,
                        ForecastRiskScore: pendingScore,
                        Tier: ClassifyTier(pendingScore)));
                }
            }

            var topPendingSorted = topPending
                .OrderByDescending(r => r.ForecastRiskScore)
                .Take(query.MaxTopPendingReleases)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                ReleaseId: release.Id.Value,
                ServiceName: release.ServiceName,
                Environment: release.Environment,
                Version: release.Version,
                CurrentStatus: release.Status,
                ForecastRiskScore: forecastScore,
                Tier: tier,
                Dimensions: dims,
                ForecastExplanation: explanation,
                RecommendedActions: actions,
                TopPendingHighRiskReleases: topPendingSorted));
        }

        // ── Dimension builder ──────────────────────────────────────────────

        private async Task<IReadOnlyList<DimensionContribution>> BuildDimensionsAsync(
            Release release,
            string tenantId,
            IReadOnlyList<Domain.Compliance.Entities.ServiceRiskProfile> riskProfiles,
            DateTimeOffset now,
            CancellationToken ct)
        {
            var from = now.AddDays(-RecentIncidentLookbackDays);

            // Dim 1: HistoricalRollbackRate (25%)
            var historicalReleases = await _releaseRepo.ListInRangeAsync(
                from, now, release.Environment, release.TenantId, ct);

            var svcHistory = historicalReleases
                .Where(r => string.Equals(r.ServiceName, release.ServiceName,
                    StringComparison.OrdinalIgnoreCase)
                    && r.Id != release.Id)
                .ToList();

            decimal rollbackRateScore;
            string rollbackExplanation;
            if (svcHistory.Count > 0)
            {
                int problematics = svcHistory.Count(r =>
                    r.Status is DeploymentStatus.RolledBack or DeploymentStatus.Failed);
                rollbackRateScore = Math.Min((decimal)problematics / svcHistory.Count * 100m, 100m);
                rollbackExplanation = problematics > 0
                    ? $"{problematics}/{svcHistory.Count} recent releases failed or rolled back"
                    : "No rollbacks in recent history";
            }
            else
            {
                rollbackRateScore = 0m;
                rollbackExplanation = "No historical release data available";
            }

            var rollbackDim = new DimensionContribution(
                "HistoricalRollbackRate",
                Math.Round(rollbackRateScore, 1),
                Math.Round(rollbackRateScore * HistoricalRollbackWeight, 1),
                rollbackExplanation);

            // Dim 2: EnvironmentInstability (20%)
            decimal envInstability = await _envInstabilityReader.GetInstabilityScoreAsync(
                tenantId, release.Environment, ct);
            envInstability = Math.Clamp(envInstability, 0m, 100m);
            string envExplanation = envInstability >= 60m
                ? $"Environment '{release.Environment}' is in degraded or critical state"
                : $"Environment '{release.Environment}' instability score: {envInstability:F1}";

            var envDim = new DimensionContribution(
                "EnvironmentInstability",
                Math.Round(envInstability, 1),
                Math.Round(envInstability * EnvironmentInstabilityWeight, 1),
                envExplanation);

            // Dim 3: ServiceRiskProfileScore (20%)
            var svcProfile = riskProfiles.FirstOrDefault(p =>
                string.Equals(p.ServiceName, release.ServiceName, StringComparison.OrdinalIgnoreCase));

            decimal riskProfileScore = svcProfile is not null ? svcProfile.OverallScore : 0m;
            string riskProfileExplanation = svcProfile is not null
                ? $"Service risk level: {svcProfile.OverallRiskLevel} (score {riskProfileScore})"
                : "No risk profile computed for this service";

            var riskProfileDim = new DimensionContribution(
                "ServiceRiskProfileScore",
                Math.Round(riskProfileScore, 1),
                Math.Round(riskProfileScore * ServiceRiskProfileWeight, 1),
                riskProfileExplanation);

            // Dim 4: ChangeConfidenceInverse (20%)
            var confidence = await _confidenceRepo.GetByReleaseIdAsync(release.Id, ct);
            decimal confidenceScore = confidence?.AggregatedScore ?? 50m;
            decimal confidenceInverse = Math.Clamp(100m - confidenceScore, 0m, 100m);
            string confidenceExplanation = confidence is not null
                ? $"Change confidence: {confidenceScore:F1}/100 (inverse risk: {confidenceInverse:F1})"
                : "No confidence assessment found — using neutral baseline (50)";

            var confidenceDim = new DimensionContribution(
                "ChangeConfidenceInverse",
                Math.Round(confidenceInverse, 1),
                Math.Round(confidenceInverse * ChangeConfidenceInverseWeight, 1),
                confidenceExplanation);

            // Dim 5: RecentIncidentRate (15%)
            int recentProblematic = svcHistory.Count(r =>
                r.Status is DeploymentStatus.Failed or DeploymentStatus.RolledBack);
            decimal incidentRate = svcHistory.Count > 0
                ? Math.Min((decimal)recentProblematic / svcHistory.Count * 100m, 100m)
                : 0m;
            string incidentExplanation = recentProblematic > 0
                ? $"{recentProblematic} failed/rolled-back deployments in last {RecentIncidentLookbackDays} days"
                : "No post-deploy failures detected recently";

            var incidentDim = new DimensionContribution(
                "RecentIncidentRate",
                Math.Round(incidentRate, 1),
                Math.Round(incidentRate * RecentIncidentRateWeight, 1),
                incidentExplanation);

            return [rollbackDim, envDim, riskProfileDim, confidenceDim, incidentDim];
        }

        // ── Explanation and actions ────────────────────────────────────────

        private static (IReadOnlyList<string> Explanation, IReadOnlyList<string> Actions)
            BuildExplanationAndActions(
                IReadOnlyList<DimensionContribution> dims,
                RiskForecastTier tier)
        {
            var topFactors = dims
                .OrderByDescending(d => d.WeightedScore)
                .Take(3)
                .Select(d => $"[{d.DimensionName}] {d.Explanation}")
                .ToList();

            var actions = new List<string>();

            if (tier is RiskForecastTier.High or RiskForecastTier.Critical)
            {
                if (dims.Any(d => d.DimensionName == "HistoricalRollbackRate" && d.RawScore >= 30m))
                    actions.Add("Review recent rollback root causes before promoting this release");

                if (dims.Any(d => d.DimensionName == "EnvironmentInstability" && d.RawScore >= 50m))
                    actions.Add("Resolve open environment drift findings before deployment");

                if (dims.Any(d => d.DimensionName == "ServiceRiskProfileScore" && d.RawScore >= 60m))
                    actions.Add("Address active risk signals in the Risk Center for this service");

                if (dims.Any(d => d.DimensionName == "ChangeConfidenceInverse" && d.RawScore >= 50m))
                    actions.Add("Improve change confidence by completing evidence pack and validation steps");

                if (tier == RiskForecastTier.Critical)
                    actions.Add("Request additional approval or schedule deployment within a planned change window");
            }

            if (actions.Count == 0)
                actions.Add("No immediate actions required — proceed with standard deployment process");

            return (topFactors, actions);
        }

        // ── Tier classifier ────────────────────────────────────────────────

        internal static RiskForecastTier ClassifyTier(decimal score) => score switch
        {
            >= CriticalThreshold => RiskForecastTier.Critical,
            >= HighThreshold => RiskForecastTier.High,
            >= ModerateThreshold => RiskForecastTier.Moderate,
            _ => RiskForecastTier.Low
        };
    }
}
