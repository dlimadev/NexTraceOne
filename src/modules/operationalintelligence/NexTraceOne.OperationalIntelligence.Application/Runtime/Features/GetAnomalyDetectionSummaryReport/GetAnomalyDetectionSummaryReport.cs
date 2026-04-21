using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetAnomalyDetectionSummaryReport;

/// <summary>
/// Feature: GetAnomalyDetectionSummaryReport — sumário consolidado de anomalias por serviço.
///
/// Agrega sinais de anomalia de 5 fontes distintas no período e responde:
/// "Quais serviços têm múltiplos sinais de problema simultâneos?"
///
/// Fontes de anomalia:
/// 1. <c>WasteSignal</c>      — idle/overprovision (FinOps)
/// 2. <c>DriftFinding</c>     — desvio de baseline (Runtime)
/// 3. <c>SloObservation</c>   — observações com status Breached (SLO)
/// 4. <c>ChaosExperiment</c>  — experimentos com status Failed (Chaos)
/// 5. <c>VulnerabilityAdvisory</c> — advisories Critical ou High (cross-module bridge)
///
/// Por serviço, conta o número de tipos de anomalia distintos e classifica por <c>AnomalyDensity</c>:
/// - <c>Clean</c>    — 0 tipos de anomalia ativos
/// - <c>Moderate</c> — 1–2 tipos de anomalia
/// - <c>Dense</c>    — 3–4 tipos de anomalia
/// - <c>Critical</c> — ≥ 5 tipos de anomalia simultâneos
///
/// Produz:
/// - <c>MultiAnomalyServices</c> — serviços com ≥ 3 tipos simultâneos (atenção imediata)
/// - <c>AnomalyTimeline</c>      — pico diário de serviços com anomalias nos últimos 30 dias
/// - distribuição por tipo de anomalia no tenant
///
/// Orienta Tech Lead, Engineer e Platform Admin como early-warning dashboard unificado.
///
/// Wave W.3 — Anomaly Detection Summary Report (OperationalIntelligence).
/// </summary>
public static class GetAnomalyDetectionSummaryReport
{
    private const int ModerateThreshold = 1;
    private const int DenseThreshold = 3;
    private const int CriticalThreshold = 5;
    private const int TimelinePoints = 30;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (7–90, default 30).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking (1–100, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int MaxTopServices = 10) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Densidade de anomalias simultâneas num serviço.</summary>
    public enum AnomalyDensity
    {
        /// <summary>Nenhuma anomalia activa no período.</summary>
        Clean,
        /// <summary>1–2 tipos de anomalia simultâneos.</summary>
        Moderate,
        /// <summary>3–4 tipos de anomalia simultâneos.</summary>
        Dense,
        /// <summary>≥ 5 tipos de anomalia simultâneos — requer atenção imediata.</summary>
        Critical
    }

    /// <summary>Distribuição por tipo de anomalia no tenant.</summary>
    public sealed record AnomalyTypeDistribution(
        int WasteSignalCount,
        int DriftFindingCount,
        int SloBreachCount,
        int ChaosFailureCount,
        int VulnerabilityCount);

    /// <summary>Ponto diário da timeline de anomalias.</summary>
    public sealed record AnomalyTimelinePoint(
        DateOnly Date,
        int ServicesWithAnomalies);

    /// <summary>Entrada de anomalias de um serviço.</summary>
    public sealed record ServiceAnomalyEntry(
        string ServiceName,
        int AnomalyCount,
        AnomalyDensity Density,
        int WasteSignals,
        int DriftFindings,
        int SloBreaches,
        int ChaosFailures,
        int Vulnerabilities);

    /// <summary>Resultado do relatório de sumário de anomalias.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesWithAnomalies,
        AnomalyTypeDistribution TypeDistribution,
        IReadOnlyList<AnomalyTimelinePoint> AnomalyTimeline,
        IReadOnlyList<ServiceAnomalyEntry> MultiAnomalyServices,
        IReadOnlyList<ServiceAnomalyEntry> TopByAnomalyCount,
        IReadOnlyList<ServiceAnomalyEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IWasteSignalRepository _wasteRepo;
        private readonly IDriftFindingRepository _driftRepo;
        private readonly ISloObservationRepository _sloRepo;
        private readonly IChaosExperimentRepository _chaosRepo;
        private readonly IVulnerabilityAdvisoryReader _vulnReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IWasteSignalRepository wasteRepo,
            IDriftFindingRepository driftRepo,
            ISloObservationRepository sloRepo,
            IChaosExperimentRepository chaosRepo,
            IVulnerabilityAdvisoryReader vulnReader,
            IDateTimeProvider clock)
        {
            _wasteRepo = Guard.Against.Null(wasteRepo);
            _driftRepo = Guard.Against.Null(driftRepo);
            _sloRepo = Guard.Against.Null(sloRepo);
            _chaosRepo = Guard.Against.Null(chaosRepo);
            _vulnReader = Guard.Against.Null(vulnReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            // 1. Fetch all anomaly sources in parallel
            var wasteTask = _wasteRepo.ListAllAsync(includeAcknowledged: false, ct: cancellationToken);
            var driftTask = _driftRepo.ListByTenantInPeriodAsync(from, now, cancellationToken);
            var sloTask = _sloRepo.ListByTenantAsync(
                query.TenantId, from, now,
                statusFilter: SloObservationStatus.Breached,
                ct: cancellationToken);
            var chaosTask = _chaosRepo.ListAsync(
                query.TenantId,
                serviceName: null,
                environment: null,
                status: ExperimentStatus.Failed,
                cancellationToken);
            var vulnTask = _vulnReader.ListCriticalOrHighServiceNamesInPeriodAsync(
                from, now, cancellationToken);

            await Task.WhenAll(wasteTask, driftTask, sloTask, chaosTask, vulnTask);

            // Filter waste signals to the period
            var wasteSignals = (await wasteTask)
                .Where(w => w.DetectedAt >= from && w.DetectedAt <= now)
                .ToList();
            var driftFindings = await driftTask;
            var sloBreaches = await sloTask;
            // Filter chaos failures to period
            var chaosFailures = (await chaosTask)
                .Where(c => c.CreatedAt >= from && c.CreatedAt <= now)
                .ToList();
            var vulnServiceNames = await vulnTask;

            // 2. Build per-service anomaly counts
            var wasteByService = wasteSignals
                .GroupBy(w => w.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var driftByService = driftFindings
                .GroupBy(d => d.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var sloByService = sloBreaches
                .GroupBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var chaosByService = chaosFailures
                .GroupBy(c => c.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var vulnByService = vulnServiceNames
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            // 3. Union all service names
            var allServiceNames = wasteByService.Keys
                .Concat(driftByService.Keys)
                .Concat(sloByService.Keys)
                .Concat(chaosByService.Keys)
                .Concat(vulnByService.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            // 4. Build entries
            var entries = new List<ServiceAnomalyEntry>(allServiceNames.Count);

            foreach (var svcName in allServiceNames)
            {
                int waste = wasteByService.GetValueOrDefault(svcName, 0);
                int drift = driftByService.GetValueOrDefault(svcName, 0);
                int slo = sloByService.GetValueOrDefault(svcName, 0);
                int chaos = chaosByService.GetValueOrDefault(svcName, 0);
                int vuln = vulnByService.GetValueOrDefault(svcName, 0);

                // AnomalyCount = number of distinct anomaly TYPES present (not total instances)
                int distinctTypes = (waste > 0 ? 1 : 0)
                    + (drift > 0 ? 1 : 0)
                    + (slo > 0 ? 1 : 0)
                    + (chaos > 0 ? 1 : 0)
                    + (vuln > 0 ? 1 : 0);

                var density = ClassifyDensity(distinctTypes);

                entries.Add(new ServiceAnomalyEntry(
                    ServiceName: svcName,
                    AnomalyCount: distinctTypes,
                    Density: density,
                    WasteSignals: waste,
                    DriftFindings: drift,
                    SloBreaches: slo,
                    ChaosFailures: chaos,
                    Vulnerabilities: vuln));
            }

            // 5. Multi-anomaly services (≥ 3 distinct anomaly types)
            var multiAnomaly = entries
                .Where(e => e.AnomalyCount >= DenseThreshold)
                .OrderByDescending(e => e.AnomalyCount)
                .ThenBy(e => e.ServiceName)
                .ToList();

            var topByCount = entries
                .Where(e => e.AnomalyCount > 0)
                .OrderByDescending(e => e.AnomalyCount)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxTopServices)
                .ToList();

            // 6. Type distribution totals
            var typeDistribution = new AnomalyTypeDistribution(
                WasteSignalCount: wasteSignals.Count,
                DriftFindingCount: driftFindings.Count,
                SloBreachCount: sloBreaches.Count,
                ChaosFailureCount: chaosFailures.Count,
                VulnerabilityCount: vulnServiceNames.Count);

            // 7. Timeline: for last 30 days, count distinct services with any anomaly per day
            var timeline = BuildTimeline(now, wasteSignals, driftFindings, sloBreaches, chaosFailures);

            int totalWithAnomalies = entries.Count(e => e.AnomalyCount > 0);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesWithAnomalies: totalWithAnomalies,
                TypeDistribution: typeDistribution,
                AnomalyTimeline: timeline,
                MultiAnomalyServices: multiAnomaly,
                TopByAnomalyCount: topByCount,
                AllServices: entries));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static AnomalyDensity ClassifyDensity(int distinctTypes) => distinctTypes switch
        {
            0 => AnomalyDensity.Clean,
            <= 2 => AnomalyDensity.Moderate,
            <= 4 => AnomalyDensity.Dense,
            _ => AnomalyDensity.Critical
        };

        private static IReadOnlyList<AnomalyTimelinePoint> BuildTimeline(
            DateTimeOffset now,
            IReadOnlyList<Domain.Cost.Entities.WasteSignal> waste,
            IReadOnlyList<Domain.Runtime.Entities.DriftFinding> drifts,
            IReadOnlyList<Domain.Runtime.Entities.SloObservation> sloBreaches,
            IReadOnlyList<Domain.Runtime.Entities.ChaosExperiment> chaosFailures)
        {
            var points = new List<AnomalyTimelinePoint>(TimelinePoints);

            for (int i = TimelinePoints - 1; i >= 0; i--)
            {
                var day = DateOnly.FromDateTime(now.AddDays(-i).DateTime);
                var dayStart = new DateTimeOffset(day.Year, day.Month, day.Day, 0, 0, 0, TimeSpan.Zero);
                var dayEnd = dayStart.AddDays(1);

                var servicesOnDay = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var w in waste)
                    if (w.DetectedAt >= dayStart && w.DetectedAt < dayEnd)
                        servicesOnDay.Add(w.ServiceName);

                foreach (var d in drifts)
                    if (d.DetectedAt >= dayStart && d.DetectedAt < dayEnd)
                        servicesOnDay.Add(d.ServiceName);

                foreach (var s in sloBreaches)
                    if (s.ObservedAt >= dayStart && s.ObservedAt < dayEnd)
                        servicesOnDay.Add(s.ServiceName);

                foreach (var c in chaosFailures)
                    if (c.CreatedAt >= dayStart && c.CreatedAt < dayEnd)
                        servicesOnDay.Add(c.ServiceName);

                points.Add(new AnomalyTimelinePoint(day, servicesOnDay.Count));
            }

            return points;
        }
    }
}
