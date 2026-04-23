using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetIncidentProbabilityReport;

/// <summary>
/// Feature: GetIncidentProbabilityReport — scoring de probabilidade de incidente por serviço.
///
/// Calcula <c>IncidentProbabilityScore</c> (0–100) por 5 sinais ponderados:
/// - <c>OpenDriftSignals</c>       (25%) — nº de DriftFindings abertos com severidade High/Critical
/// - <c>SloBreachTrend</c>         (25%) — % de observações SLO em Breached nas últimas 72h
/// - <c>ChaosGap</c>               (20%) — serviço sem FullCoverage de chaos
/// - <c>RecentHighRiskRelease</c>  (20%) — release com ForecastRiskScore > threshold nas últimas 24h
/// - <c>OpenVulnerabilities</c>    (10%) — serviços com vulnerabilidades Critical/High
///
/// Classifica por <c>IncidentProbabilityTier</c>:
/// - <c>Unlikely</c>  — score &lt; 20
/// - <c>Possible</c>  — score &lt; 40
/// - <c>Probable</c>  — score &lt; 65
/// - <c>Imminent</c>  — score ≥ 65 (alerta proactivo recomendado)
///
/// Inclui:
/// - <c>ProbabilityExplanation</c>  — top 3 factores por serviço
/// - <c>TenantRiskHeatmap</c>       — distribuição por tier e top 10 serviços
/// - <c>AlertServicesList</c>       — serviços Imminent com drill-down
///
/// Orientado para Engineer, Tech Lead e Platform Admin como early warning system.
///
/// Wave AI.3 — Incident Probability Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetIncidentProbabilityReport
{
    // ── Signal weights ──────────────────────────────────────────────────────
    private const decimal OpenDriftWeight = 0.25m;
    private const decimal SloBreachWeight = 0.25m;
    private const decimal ChaosGapWeight = 0.20m;
    private const decimal HighRiskReleaseWeight = 0.20m;
    private const decimal OpenVulnWeight = 0.10m;

    // ── Tier thresholds ─────────────────────────────────────────────────────
    internal const decimal ImminentThreshold = 65m;
    internal const decimal ProbableThreshold = 40m;
    internal const decimal PossibleThreshold = 20m;

    // ── Signal constants ────────────────────────────────────────────────────
    private const int SloBreach72hLookback = 72;
    private const int HighRiskRelease24hLookback = 24;
    private const decimal HighRiskForecastScoreThreshold = 75m;
    private const int MaxDriftContribution = 5;   // cap: 5 open drifts = 100%
    private const int MaxVulnContribution = 5;    // cap: 5 open vulns = 100%

    private const int TopServicesDefault = 10;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente.</para>
    /// <para><c>MaxTopServices</c>: serviços no ranking (1–100, default 10).</para>
    /// <para><c>ImminentThresholdOverride</c>: threshold personalizado para tier Imminent (null = usar default 65).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string? Environment = null,
        int MaxTopServices = TopServicesDefault,
        decimal? ImminentThresholdOverride = null) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de probabilidade de incidente por serviço nas próximas 48–72 horas.</summary>
    public enum IncidentProbabilityTier
    {
        /// <summary>Score &lt; 20 — baixa probabilidade, operação estável.</summary>
        Unlikely,
        /// <summary>Score &lt; 40 — possível, alguns sinais de atenção.</summary>
        Possible,
        /// <summary>Score &lt; 65 — provável, múltiplos sinais convergentes.</summary>
        Probable,
        /// <summary>Score ≥ 65 — iminente, alerta proactivo recomendado.</summary>
        Imminent
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição de serviços por tier de probabilidade de incidente.</summary>
    public sealed record TierDistribution(
        int UnlikelyCount,
        int PossibleCount,
        int ProbableCount,
        int ImminentCount);

    /// <summary>Sinal de risco individual para um serviço.</summary>
    public sealed record RiskSignal(
        string SignalName,
        decimal RawScore,
        decimal WeightedScore,
        string Explanation);

    /// <summary>Entrada de probabilidade de incidente para um serviço.</summary>
    public sealed record ServiceIncidentProbabilityEntry(
        string ServiceName,
        string Environment,
        decimal IncidentProbabilityScore,
        IncidentProbabilityTier Tier,
        IReadOnlyList<RiskSignal> Signals,
        IReadOnlyList<string> ProbabilityExplanation);

    /// <summary>Mapa de risco do tenant.</summary>
    public sealed record TenantRiskHeatmap(
        TierDistribution Distribution,
        decimal ImminentPct,
        decimal ProbablePct,
        decimal PossiblePct,
        decimal UnlikelyPct,
        IReadOnlyList<ServiceIncidentProbabilityEntry> Top10RiskiestServices);

    /// <summary>Resultado do relatório de probabilidade de incidente.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string? Environment,
        int TotalServicesAnalyzed,
        TenantRiskHeatmap RiskHeatmap,
        IReadOnlyList<ServiceIncidentProbabilityEntry> AlertServicesList,
        IReadOnlyList<ServiceIncidentProbabilityEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
            When(q => q.ImminentThresholdOverride.HasValue, () =>
                RuleFor(q => q.ImminentThresholdOverride!.Value).InclusiveBetween(20m, 90m));
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IDriftFindingRepository _driftRepo;
        private readonly ISloObservationRepository _sloRepo;
        private readonly IChaosExperimentRepository _chaosRepo;
        private readonly IVulnerabilityAdvisoryReader _vulnReader;
        private readonly IDeploymentRiskForecastReader _forecastReader;
        private readonly IActiveServiceNamesReader _serviceNamesReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IDriftFindingRepository driftRepo,
            ISloObservationRepository sloRepo,
            IChaosExperimentRepository chaosRepo,
            IVulnerabilityAdvisoryReader vulnReader,
            IDeploymentRiskForecastReader forecastReader,
            IActiveServiceNamesReader serviceNamesReader,
            IDateTimeProvider clock)
        {
            _driftRepo = Guard.Against.Null(driftRepo);
            _sloRepo = Guard.Against.Null(sloRepo);
            _chaosRepo = Guard.Against.Null(chaosRepo);
            _vulnReader = Guard.Against.Null(vulnReader);
            _forecastReader = Guard.Against.Null(forecastReader);
            _serviceNamesReader = Guard.Against.Null(serviceNamesReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            decimal imminentThreshold = query.ImminentThresholdOverride ?? ImminentThreshold;

            // 1. Determine active services
            var knownServices = await _serviceNamesReader.ListActiveServiceNamesAsync(
                query.TenantId, cancellationToken);

            // 2. Load shared signals that apply across all services
            var driftSince = now.AddDays(-7);
            var allDriftFindings = await _driftRepo.ListByTenantInPeriodAsync(
                driftSince, now, cancellationToken);

            var sloSince = now.AddHours(-SloBreach72hLookback);
            var allSloObs = await _sloRepo.ListByTenantAsync(
                query.TenantId, sloSince, now, ct: cancellationToken);

            var chaosAll = await _chaosRepo.ListAsync(
                query.TenantId, serviceName: null, environment: null, status: null, cancellationToken);

            var vulnSince = now.AddDays(-30);
            var vulnServiceNames = await _vulnReader.ListCriticalOrHighServiceNamesInPeriodAsync(
                vulnSince, now, cancellationToken);
            var vulnCountByService = vulnServiceNames
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            // 3. Filter by environment if requested
            var openDriftByService = allDriftFindings
                .Where(d => d.IsOpen
                    && (query.Environment is null || string.Equals(d.Environment, query.Environment,
                        StringComparison.OrdinalIgnoreCase))
                    && d.Severity >= DriftSeverity.High)
                .GroupBy(d => d.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var sloByService = allSloObs
                .Where(s => query.Environment is null || string.Equals(s.Environment, query.Environment,
                    StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var chaosProductionFullCoverage = chaosAll
                .Where(e => string.Equals(e.Environment, "production", StringComparison.OrdinalIgnoreCase)
                    && e.Status == Domain.Runtime.Enums.ExperimentStatus.Completed)
                .Select(e => e.ServiceName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 4. Determine service list (bridge or chaos/drift fallback)
            IEnumerable<string> serviceNames = knownServices.Count > 0
                ? knownServices
                : allDriftFindings
                    .Select(d => d.ServiceName)
                    .Concat(allSloObs.Select(s => s.ServiceName))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

            var orderedServiceNames = serviceNames
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .OrderBy(s => s)
                .ToList();

            if (orderedServiceNames.Count == 0)
                return Result<Report>.Success(EmptyReport(now, query));

            // 5. Build per-service entries
            var entries = new List<ServiceIncidentProbabilityEntry>(orderedServiceNames.Count);

            foreach (var svcName in orderedServiceNames)
            {
                var entry = await BuildEntryAsync(
                    svcName,
                    query.TenantId,
                    query.Environment,
                    now,
                    imminentThreshold,
                    openDriftByService,
                    sloByService,
                    chaosProductionFullCoverage,
                    vulnCountByService,
                    cancellationToken);
                entries.Add(entry);
            }

            // 6. Build output
            var allSorted = entries
                .OrderByDescending(e => e.IncidentProbabilityScore)
                .ThenBy(e => e.ServiceName)
                .ToList();

            var alertList = entries
                .Where(e => e.Tier == IncidentProbabilityTier.Imminent)
                .OrderByDescending(e => e.IncidentProbabilityScore)
                .ToList();

            var heatmap = BuildHeatmap(entries, query.MaxTopServices, imminentThreshold);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                Environment: query.Environment,
                TotalServicesAnalyzed: entries.Count,
                RiskHeatmap: heatmap,
                AlertServicesList: alertList,
                AllServices: allSorted));
        }

        // ── Entry builder ──────────────────────────────────────────────────

        private async Task<ServiceIncidentProbabilityEntry> BuildEntryAsync(
            string serviceName,
            string tenantId,
            string? environment,
            DateTimeOffset now,
            decimal imminentThreshold,
            Dictionary<string, int> openDriftByService,
            Dictionary<string, List<Domain.Runtime.Entities.SloObservation>> sloByService,
            HashSet<string> chaosProductionFullCoverage,
            Dictionary<string, int> vulnCountByService,
            CancellationToken ct)
        {
            // Signal 1: OpenDriftSignals (25%)
            int openDrifts = openDriftByService.GetValueOrDefault(serviceName, 0);
            decimal driftRaw = Math.Min((decimal)openDrifts / MaxDriftContribution * 100m, 100m);
            var driftSignal = new RiskSignal(
                "OpenDriftSignals",
                Math.Round(driftRaw, 1),
                Math.Round(driftRaw * OpenDriftWeight, 1),
                openDrifts > 0
                    ? $"{openDrifts} open drift finding(s) with High/Critical severity"
                    : "No open high-severity drift findings");

            // Signal 2: SloBreachTrend (25%)
            decimal sloBreachRaw = 0m;
            string sloExplanation;
            if (sloByService.TryGetValue(serviceName, out var sloObs) && sloObs.Count > 0)
            {
                int breachedCount = sloObs.Count(s => s.Status == SloObservationStatus.Breached);
                sloBreachRaw = Math.Min((decimal)breachedCount / sloObs.Count * 100m, 100m);
                sloExplanation = breachedCount > 0
                    ? $"{breachedCount}/{sloObs.Count} SLO observations breached in last 72h"
                    : "No SLO breaches in last 72h";
            }
            else
            {
                sloExplanation = "No SLO observations in last 72h";
            }

            var sloSignal = new RiskSignal(
                "SloBreachTrend",
                Math.Round(sloBreachRaw, 1),
                Math.Round(sloBreachRaw * SloBreachWeight, 1),
                sloExplanation);

            // Signal 3: ChaosGap (20%)
            bool hasChaosFullCoverage = chaosProductionFullCoverage.Contains(serviceName);
            decimal chaosRaw = hasChaosFullCoverage ? 0m : 100m;
            var chaosSignal = new RiskSignal(
                "ChaosGap",
                chaosRaw,
                Math.Round(chaosRaw * ChaosGapWeight, 1),
                hasChaosFullCoverage
                    ? "Service has completed chaos experiment in production"
                    : "No completed chaos experiment in production");

            // Signal 4: RecentHighRiskRelease (20%)
            decimal forecastScore = await _forecastReader.GetMaxRecentForecastRiskScoreAsync(
                tenantId, serviceName, HighRiskRelease24hLookback, ct);
            bool hasHighRiskRelease = forecastScore >= HighRiskForecastScoreThreshold;
            decimal releaseRaw = hasHighRiskRelease ? 100m : 0m;
            var releaseSignal = new RiskSignal(
                "RecentHighRiskRelease",
                releaseRaw,
                Math.Round(releaseRaw * HighRiskReleaseWeight, 1),
                hasHighRiskRelease
                    ? $"Recent release has high forecast risk score ({forecastScore:F1})"
                    : "No high-risk releases in the last 24h");

            // Signal 5: OpenVulnerabilities (10%)
            int vulnCount = vulnCountByService.GetValueOrDefault(serviceName, 0);
            decimal vulnRaw = Math.Min((decimal)vulnCount / MaxVulnContribution * 100m, 100m);
            var vulnSignal = new RiskSignal(
                "OpenVulnerabilities",
                Math.Round(vulnRaw, 1),
                Math.Round(vulnRaw * OpenVulnWeight, 1),
                vulnCount > 0
                    ? $"{vulnCount} open Critical/High vulnerability advisory records"
                    : "No open critical/high vulnerabilities");

            IReadOnlyList<RiskSignal> signals = [driftSignal, sloSignal, chaosSignal, releaseSignal, vulnSignal];

            decimal totalScore = Math.Round(
                Math.Clamp(signals.Sum(s => s.WeightedScore), 0m, 100m), 1);

            var tier = ClassifyTier(totalScore, imminentThreshold);

            var explanation = signals
                .OrderByDescending(s => s.WeightedScore)
                .Take(3)
                .Select(s => $"[{s.SignalName}] {s.Explanation}")
                .ToList();

            return new ServiceIncidentProbabilityEntry(
                ServiceName: serviceName,
                Environment: environment ?? "all",
                IncidentProbabilityScore: totalScore,
                Tier: tier,
                Signals: signals,
                ProbabilityExplanation: explanation);
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static IncidentProbabilityTier ClassifyTier(decimal score, decimal imminentThreshold) =>
            score switch
            {
                var s when s >= imminentThreshold => IncidentProbabilityTier.Imminent,
                >= ProbableThreshold => IncidentProbabilityTier.Probable,
                >= PossibleThreshold => IncidentProbabilityTier.Possible,
                _ => IncidentProbabilityTier.Unlikely
            };

        private static TenantRiskHeatmap BuildHeatmap(
            IReadOnlyList<ServiceIncidentProbabilityEntry> entries,
            int maxTop,
            decimal imminentThreshold)
        {
            int total = entries.Count;
            int imminentCount = entries.Count(e => e.Tier == IncidentProbabilityTier.Imminent);
            int probableCount = entries.Count(e => e.Tier == IncidentProbabilityTier.Probable);
            int possibleCount = entries.Count(e => e.Tier == IncidentProbabilityTier.Possible);
            int unlikelyCount = entries.Count(e => e.Tier == IncidentProbabilityTier.Unlikely);

            decimal imminentPct = total > 0 ? Math.Round((decimal)imminentCount / total * 100m, 1) : 0m;
            decimal probablePct = total > 0 ? Math.Round((decimal)probableCount / total * 100m, 1) : 0m;
            decimal possiblePct = total > 0 ? Math.Round((decimal)possibleCount / total * 100m, 1) : 0m;
            decimal unlikelyPct = total > 0 ? Math.Round((decimal)unlikelyCount / total * 100m, 1) : 0m;

            var top10 = entries
                .OrderByDescending(e => e.IncidentProbabilityScore)
                .Take(maxTop)
                .ToList();

            return new TenantRiskHeatmap(
                Distribution: new TierDistribution(unlikelyCount, possibleCount, probableCount, imminentCount),
                ImminentPct: imminentPct,
                ProbablePct: probablePct,
                PossiblePct: possiblePct,
                UnlikelyPct: unlikelyPct,
                Top10RiskiestServices: top10);
        }

        private static Report EmptyReport(DateTimeOffset now, Query query) => new(
            GeneratedAt: now,
            Environment: query.Environment,
            TotalServicesAnalyzed: 0,
            RiskHeatmap: new TenantRiskHeatmap(
                new TierDistribution(0, 0, 0, 0), 0m, 0m, 0m, 0m, []),
            AlertServicesList: [],
            AllServices: []);
    }
}
