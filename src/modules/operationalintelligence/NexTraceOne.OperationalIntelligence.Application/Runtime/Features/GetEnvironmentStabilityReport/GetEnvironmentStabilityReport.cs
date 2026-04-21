using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetEnvironmentStabilityReport;

/// <summary>
/// Feature: GetEnvironmentStabilityReport — score de estabilidade comparado por ambiente.
///
/// Avalia cada ambiente activo no tenant (derivados das observações de SLO e experimentos
/// de chaos) contra 4 dimensões ponderadas:
/// - SLO Compliance (40%): proporção de observações SLO em estado Met vs. total no ambiente
/// - Drift-Free Rate (30%): ausência de drift findings não reconhecidos por serviço/ambiente
/// - Chaos Success Rate (20%): proporção de experimentos de chaos Completed vs. total no ambiente
/// - Incident-Free Rate (10%): ausência de SLO breaches prolongadas (proxy de incidente pós-deploy)
///
/// Classifica cada ambiente em <c>StabilityTier</c>:
/// - <c>Stable</c>    — score ≥ 80
/// - <c>Unstable</c>  — score ≥ 55
/// - <c>Critical</c>  — score &lt; 55
///
/// Produz um flag de alerta <c>NonProdMoreUnstableThanProd</c> quando um ambiente
/// de não-produção tem score inferior ao de produção — indicador de que o ambiente de
/// validação pré-produção tem menor confiança do que o ambiente de produção.
///
/// Orienta Tech Lead, Architect e Platform Admin a priorizar a estabilização de ambientes
/// de pré-produção antes de promover mudanças.
///
/// Wave T.3 — Environment Stability Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetEnvironmentStabilityReport
{
    // ── Dimension weights ──────────────────────────────────────────────────
    private const decimal SloWeight = 0.40m;
    private const decimal DriftWeight = 0.30m;
    private const decimal ChaosWeight = 0.20m;
    private const decimal IncidentFreeWeight = 0.10m;

    // ── StabilityTier thresholds ───────────────────────────────────────────
    private const decimal StableThreshold = 80m;
    private const decimal UnstableThreshold = 55m;

    // ── Neutral score applied when no data is available for a dimension ─────
    private const decimal NeutralScore = 60m;

    // ── Production environment keywords ───────────────────────────────────
    private static readonly HashSet<string> ProductionKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "prod", "production", "prd" };

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–90, default 30).</para>
    /// <para><c>MaxDriftPenaltyPerService</c>: pontos subtraídos por drift finding não reconhecido por serviço (1–20, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int MaxDriftPenaltyPerService = 10) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de estabilidade de um ambiente.</summary>
    public enum StabilityTier
    {
        /// <summary>Score ≥ 80 — ambiente estável e confiável.</summary>
        Stable,
        /// <summary>Score ≥ 55 — ambiente com problemas a resolver.</summary>
        Unstable,
        /// <summary>Score &lt; 55 — ambiente com degradação crítica.</summary>
        Critical
    }

    /// <summary>Distribuição de ambientes por tier de estabilidade.</summary>
    public sealed record StabilityTierDistribution(
        int StableCount,
        int UnstableCount,
        int CriticalCount);

    /// <summary>Detalhe dimensional de estabilidade de um ambiente.</summary>
    public sealed record StabilityDimensions(
        decimal SloComplianceRatePct,
        decimal SloScore,
        int TotalSloObservations,
        int SloBreachedCount,
        decimal DriftFreeRatePct,
        decimal DriftScore,
        int UnacknowledgedDriftCount,
        decimal ChaosSuccessRatePct,
        decimal ChaosScore,
        int TotalChaosExperiments,
        decimal IncidentFreeRatePct,
        decimal IncidentFreeScore,
        int TotalBreachEventsAsProxy);

    /// <summary>Resultado de estabilidade para um ambiente.</summary>
    public sealed record EnvironmentStabilityEntry(
        string Environment,
        bool IsProduction,
        decimal StabilityScore,
        StabilityTier Tier,
        StabilityDimensions Dimensions);

    /// <summary>Resultado do relatório de estabilidade por ambiente.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalEnvironmentsAnalyzed,
        bool NonProdMoreUnstableThanProd,
        StabilityTierDistribution TierDistribution,
        IReadOnlyList<EnvironmentStabilityEntry> Environments);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(q => q.MaxDriftPenaltyPerService).InclusiveBetween(1, 20);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ISloObservationRepository _sloRepo;
        private readonly IDriftFindingRepository _driftRepo;
        private readonly IChaosExperimentRepository _chaosRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            ISloObservationRepository sloRepo,
            IDriftFindingRepository driftRepo,
            IChaosExperimentRepository chaosRepo,
            IDateTimeProvider clock)
        {
            _sloRepo = Guard.Against.Null(sloRepo);
            _driftRepo = Guard.Against.Null(driftRepo);
            _chaosRepo = Guard.Against.Null(chaosRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            // 1. Fetch all SLO observations for the tenant in the period
            var allSloObs = await _sloRepo.ListByTenantAsync(
                query.TenantId, since: from, until: now,
                statusFilter: null, ct: cancellationToken);

            // 2. Fetch all unacknowledged drift findings
            var driftFindings = await _driftRepo.ListUnacknowledgedAsync(1, 1000, cancellationToken);

            // 3. Derive environments from SLO observations
            var environments = allSloObs
                .Select(o => o.Environment)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Also include environments from chaos experiments (merged later)
            var allChaos = await _chaosRepo.ListAsync(
                query.TenantId,
                serviceName: null,
                environment: null,
                status: null,
                cancellationToken: cancellationToken);

            var recentChaos = allChaos
                .Where(e => (e.StartedAt ?? e.CreatedAt) >= from)
                .ToList();

            var chaosEnvironments = recentChaos
                .Select(e => e.Environment)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            environments = environments
                .Union(chaosEnvironments, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (environments.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalEnvironmentsAnalyzed: 0,
                    NonProdMoreUnstableThanProd: false,
                    TierDistribution: new StabilityTierDistribution(0, 0, 0),
                    Environments: []));
            }

            var entries = new List<EnvironmentStabilityEntry>();

            foreach (var env in environments)
            {
                var envSlos = allSloObs
                    .Where(o => string.Equals(o.Environment, env, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var envChaos = recentChaos
                    .Where(e => string.Equals(e.Environment, env, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var envDrift = driftFindings
                    .Where(d => string.Equals(d.Environment, env, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var dims = ComputeDimensions(
                    envSlos, envChaos, envDrift,
                    query.MaxDriftPenaltyPerService);

                decimal compositeScore = Math.Round(
                    dims.SloScore * SloWeight +
                    dims.DriftScore * DriftWeight +
                    dims.ChaosScore * ChaosWeight +
                    dims.IncidentFreeScore * IncidentFreeWeight,
                    2);

                bool isProd = ProductionKeywords.Contains(env);

                entries.Add(new EnvironmentStabilityEntry(
                    Environment: env,
                    IsProduction: isProd,
                    StabilityScore: compositeScore,
                    Tier: ClassifyTier(compositeScore),
                    Dimensions: dims));
            }

            int stableCount = entries.Count(e => e.Tier == StabilityTier.Stable);
            int unstableCount = entries.Count(e => e.Tier == StabilityTier.Unstable);
            int criticalCount = entries.Count(e => e.Tier == StabilityTier.Critical);

            // Flag: any non-prod environment has lower score than the lowest prod score
            bool nonProdMoreUnstable = DetectNonProdMoreUnstableThanProd(entries);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalEnvironmentsAnalyzed: entries.Count,
                NonProdMoreUnstableThanProd: nonProdMoreUnstable,
                TierDistribution: new StabilityTierDistribution(stableCount, unstableCount, criticalCount),
                Environments: entries.OrderBy(e => e.IsProduction ? 0 : 1).ThenBy(e => e.Environment).ToList()));
        }

        // ── Computation helpers ────────────────────────────────────────────

        private static StabilityDimensions ComputeDimensions(
            IReadOnlyList<NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities.SloObservation> sloObs,
            IReadOnlyList<NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities.ChaosExperiment> chaosExps,
            IReadOnlyList<NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities.DriftFinding> driftFindings,
            int maxDriftPenalty)
        {
            // ── SLO dimension ──────────────────────────────────────────────
            int sloTotal = sloObs.Count;
            int sloMet = sloObs.Count(o => o.Status == SloObservationStatus.Met);
            int sloBreached = sloObs.Count(o => o.Status == SloObservationStatus.Breached);
            decimal sloComplianceRate = sloTotal > 0
                ? Math.Round((decimal)sloMet / sloTotal * 100m, 2)
                : 100m;
            decimal sloScore = sloTotal > 0 ? sloComplianceRate : NeutralScore;

            // ── Drift dimension ────────────────────────────────────────────
            // Group by service to compute drift-free rate per service
            int distinctServices = driftFindings.Select(d => d.ServiceName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            int driftCount = driftFindings.Count;
            decimal driftFreeRate = driftCount == 0 ? 100m : 0m;
            decimal driftScore = driftCount == 0
                ? 100m
                : Math.Max(0m, 100m - driftCount * maxDriftPenalty);

            // ── Chaos dimension ────────────────────────────────────────────
            int chaosTotal = chaosExps.Count;
            int chaosCompleted = chaosExps.Count(e => e.Status == ExperimentStatus.Completed);
            int chaosFailed = chaosExps.Count(e => e.Status == ExperimentStatus.Failed);
            decimal chaosSuccessRate = (chaosCompleted + chaosFailed) > 0
                ? Math.Round((decimal)chaosCompleted / (chaosCompleted + chaosFailed) * 100m, 2)
                : 100m;
            decimal chaosScore = chaosTotal > 0 ? chaosSuccessRate : NeutralScore;

            // ── Incident-Free dimension (proxy: prolonged SLO breaches) ────
            // A prolonged breach (>= 2 consecutive Breached observations for same service)
            // is treated as a proxy for a post-change incident
            int breachProxyEvents = CountProlongedBreachEvents(sloObs);
            decimal incidentFreeRate = breachProxyEvents == 0 ? 100m
                : Math.Max(0m, 100m - breachProxyEvents * 20m);
            decimal incidentFreeScore = sloTotal > 0 ? incidentFreeRate : NeutralScore;

            return new StabilityDimensions(
                SloComplianceRatePct: sloComplianceRate,
                SloScore: Math.Round(sloScore, 2),
                TotalSloObservations: sloTotal,
                SloBreachedCount: sloBreached,
                DriftFreeRatePct: Math.Round(driftFreeRate, 2),
                DriftScore: Math.Round(driftScore, 2),
                UnacknowledgedDriftCount: driftCount,
                ChaosSuccessRatePct: Math.Round(chaosSuccessRate, 2),
                ChaosScore: Math.Round(chaosScore, 2),
                TotalChaosExperiments: chaosTotal,
                IncidentFreeRatePct: Math.Round(incidentFreeRate, 2),
                IncidentFreeScore: Math.Round(incidentFreeScore, 2),
                TotalBreachEventsAsProxy: breachProxyEvents);
        }

        private static int CountProlongedBreachEvents(
            IReadOnlyList<NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities.SloObservation> observations)
        {
            // Group by service and count services with ≥ 2 consecutive Breached observations
            var byService = observations.GroupBy(o => o.ServiceName, StringComparer.OrdinalIgnoreCase);
            int count = 0;
            foreach (var group in byService)
            {
                var sorted = group.OrderBy(o => o.ObservedAt).ToList();
                int consecutive = 0;
                foreach (var obs in sorted)
                {
                    if (obs.Status == SloObservationStatus.Breached)
                    {
                        consecutive++;
                        if (consecutive >= 2)
                        {
                            count++;
                            consecutive = 0; // reset to avoid double-counting
                        }
                    }
                    else
                    {
                        consecutive = 0;
                    }
                }
            }
            return count;
        }

        private static StabilityTier ClassifyTier(decimal score) => score switch
        {
            >= StableThreshold => StabilityTier.Stable,
            >= UnstableThreshold => StabilityTier.Unstable,
            _ => StabilityTier.Critical
        };

        private static bool DetectNonProdMoreUnstableThanProd(
            IReadOnlyList<EnvironmentStabilityEntry> entries)
        {
            var prodEntries = entries.Where(e => e.IsProduction).ToList();
            var nonProdEntries = entries.Where(e => !e.IsProduction).ToList();

            if (prodEntries.Count == 0 || nonProdEntries.Count == 0)
                return false;

            decimal minProdScore = prodEntries.Min(e => e.StabilityScore);
            decimal minNonProdScore = nonProdEntries.Min(e => e.StabilityScore);

            return minNonProdScore < minProdScore;
        }
    }
}
