using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPostIncidentLearningReport;

/// <summary>
/// Feature: GetPostIncidentLearningReport — aprendizado organizacional pós-incidente.
///
/// Quantifica em que medida os incidentes geram conhecimento documentado (runbooks aprovados)
/// que previne recorrência. Usa releases com eventos "incident_correlated" como proxy de
/// incidentes no sistema de Change Governance e um leitor de runbooks aprovados por serviço
/// (abstracção bounded-context-safe via <see cref="IIncidentLearningReader"/>).
///
/// Classifica a cobertura de aprendizado por serviço em:
/// - <c>Full</c>    — taxa de incidentes com runbook aprovado ≥ <c>FullCoverageThreshold</c> (default 80%)
/// - <c>Partial</c> — taxa ≥ <c>PartialCoverageThreshold</c> (default 40%)
/// - <c>Low</c>     — taxa &lt; <c>PartialCoverageThreshold</c>
///
/// Produz:
/// - totais de incidentes e taxa de learning rate global
/// - contagem de incidentes recorrentes não documentados (mesmo serviço, ≥ 2 ocorrências, sem runbook)
/// - distribuição de serviços por <c>LearningCoverage</c>
/// - top serviços com menor learning rate
/// - lista de serviços com incidentes recorrentes não documentados
///
/// Orienta Tech Lead, Architect e Platform Admin a identificar pontos cegos de conhecimento
/// operacional e priorizar a geração de runbooks a partir de incidentes resolvidos.
///
/// Wave T.1 — Post-Incident Learning Report (ChangeGovernance Compliance).
/// </summary>
public static class GetPostIncidentLearningReport
{
    private const string IncidentCorrelatedEventType = "incident_correlated";

    // ── Thresholds por defeito ─────────────────────────────────────────────
    private const decimal DefaultFullCoverageThresholdPct = 80m;
    private const decimal DefaultPartialCoverageThresholdPct = 40m;
    private const int DefaultRecurringIncidentMinCount = 2;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (7–180, default 90).</para>
    /// <para><c>TopServiceCount</c>: número máximo de serviços a listar no ranking (1–100, default 10).</para>
    /// <para><c>FullCoverageThresholdPct</c>: threshold para classificação Full (default 80).</para>
    /// <para><c>PartialCoverageThresholdPct</c>: threshold para classificação Partial (default 40).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int TopServiceCount = 10,
        decimal FullCoverageThresholdPct = DefaultFullCoverageThresholdPct,
        decimal PartialCoverageThresholdPct = DefaultPartialCoverageThresholdPct,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de cobertura de aprendizado pós-incidente por serviço.</summary>
    public enum LearningCoverage
    {
        /// <summary>Taxa de incidentes com runbook aprovado ≥ threshold Full.</summary>
        Full,
        /// <summary>Taxa de incidentes com runbook aprovado ≥ threshold Partial.</summary>
        Partial,
        /// <summary>Taxa de incidentes com runbook aprovado &lt; threshold Partial.</summary>
        Low
    }

    /// <summary>Distribuição de serviços por nível de cobertura de aprendizado.</summary>
    public sealed record LearningCoverageDistribution(
        int FullCount,
        int PartialCount,
        int LowCount);

    /// <summary>Métricas de aprendizado pós-incidente para um serviço.</summary>
    public sealed record ServiceLearningEntry(
        string ServiceName,
        int TotalIncidents,
        int IncidentsWithRunbook,
        int RecurringIncidentsWithoutRunbook,
        decimal LearningRatePct,
        LearningCoverage Coverage);

    /// <summary>Resultado do relatório de aprendizado pós-incidente.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalIncidentsAnalyzed,
        int TotalIncidentsWithRunbook,
        decimal TenantLearningRatePct,
        int TotalRecurringWithoutRunbook,
        LearningCoverageDistribution CoverageDistribution,
        IReadOnlyList<ServiceLearningEntry> TopLowCoverageServices,
        IReadOnlyList<ServiceLearningEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 180);
            RuleFor(q => q.TopServiceCount).InclusiveBetween(1, 100);
            RuleFor(q => q.FullCoverageThresholdPct).InclusiveBetween(1m, 100m);
            RuleFor(q => q.PartialCoverageThresholdPct).InclusiveBetween(1m, 100m);
            RuleFor(q => q.PartialCoverageThresholdPct).LessThan(q => q.FullCoverageThresholdPct);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IChangeEventRepository _changeEventRepo;
        private readonly IIncidentLearningReader _learningReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IChangeEventRepository changeEventRepo,
            IIncidentLearningReader learningReader,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _changeEventRepo = Guard.Against.Null(changeEventRepo);
            _learningReader = Guard.Against.Null(learningReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            var tenantId = Guid.Parse(query.TenantId);

            // 1. Fetch releases in period
            var releases = await _releaseRepo.ListInRangeAsync(
                from, now, query.Environment, tenantId, cancellationToken);

            if (releases.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalIncidentsAnalyzed: 0,
                    TotalIncidentsWithRunbook: 0,
                    TenantLearningRatePct: 0m,
                    TotalRecurringWithoutRunbook: 0,
                    CoverageDistribution: new LearningCoverageDistribution(0, 0, 0),
                    TopLowCoverageServices: [],
                    AllServices: []));
            }

            // 2. Identify releases with incident_correlated events (proxy for incidents)
            //    service → list of release IDs that had incidents
            var serviceIncidentMap = new Dictionary<string, List<bool>>(StringComparer.OrdinalIgnoreCase);

            foreach (var release in releases)
            {
                var incidentEvents = await _changeEventRepo.ListByReleaseIdAndEventTypeAsync(
                    release.Id, IncidentCorrelatedEventType, cancellationToken);

                bool hasIncident = incidentEvents.Count > 0;

                if (!serviceIncidentMap.TryGetValue(release.ServiceName, out var list))
                {
                    list = [];
                    serviceIncidentMap[release.ServiceName] = list;
                }

                if (hasIncident)
                    list.Add(true);
            }

            if (serviceIncidentMap.Count == 0 || serviceIncidentMap.Values.All(v => v.Count == 0))
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalIncidentsAnalyzed: 0,
                    TotalIncidentsWithRunbook: 0,
                    TenantLearningRatePct: 0m,
                    TotalRecurringWithoutRunbook: 0,
                    CoverageDistribution: new LearningCoverageDistribution(0, 0, 0),
                    TopLowCoverageServices: [],
                    AllServices: []));
            }

            // 3. Fetch approved runbooks per service in the period
            var approvedRunbookServices = await _learningReader.ListServicesWithApprovedRunbookAsync(
                query.TenantId, from, now, cancellationToken);

            var approvedSet = approvedRunbookServices.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 4. Build per-service entries (only services that had incidents)
            var entries = new List<ServiceLearningEntry>();

            foreach (var (serviceName, incidentOccurrences) in serviceIncidentMap)
            {
                int total = incidentOccurrences.Count;
                if (total == 0) continue;

                bool hasRunbook = approvedSet.Contains(serviceName);
                int withRunbook = hasRunbook ? total : 0;

                // Recurring without runbook: service with ≥ 2 incidents and no approved runbook
                int recurringWithoutRunbook = (!hasRunbook && total >= DefaultRecurringIncidentMinCount) ? total : 0;

                decimal rate = total > 0
                    ? Math.Round((decimal)withRunbook / total * 100m, 2)
                    : 0m;

                entries.Add(new ServiceLearningEntry(
                    ServiceName: serviceName,
                    TotalIncidents: total,
                    IncidentsWithRunbook: withRunbook,
                    RecurringIncidentsWithoutRunbook: recurringWithoutRunbook,
                    LearningRatePct: rate,
                    Coverage: ClassifyCoverage(rate, query.FullCoverageThresholdPct, query.PartialCoverageThresholdPct)));
            }

            int globalTotal = entries.Sum(e => e.TotalIncidents);
            int globalWithRunbook = entries.Sum(e => e.IncidentsWithRunbook);
            int globalRecurring = entries.Sum(e => e.RecurringIncidentsWithoutRunbook);

            decimal tenantRate = globalTotal > 0
                ? Math.Round((decimal)globalWithRunbook / globalTotal * 100m, 2)
                : 0m;

            int fullCount = entries.Count(e => e.Coverage == LearningCoverage.Full);
            int partialCount = entries.Count(e => e.Coverage == LearningCoverage.Partial);
            int lowCount = entries.Count(e => e.Coverage == LearningCoverage.Low);

            var topLow = entries
                .OrderBy(e => e.LearningRatePct)
                .ThenByDescending(e => e.TotalIncidents)
                .Take(query.TopServiceCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalIncidentsAnalyzed: globalTotal,
                TotalIncidentsWithRunbook: globalWithRunbook,
                TenantLearningRatePct: tenantRate,
                TotalRecurringWithoutRunbook: globalRecurring,
                CoverageDistribution: new LearningCoverageDistribution(fullCount, partialCount, lowCount),
                TopLowCoverageServices: topLow,
                AllServices: entries.OrderBy(e => e.ServiceName).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static LearningCoverage ClassifyCoverage(
            decimal ratePct,
            decimal fullThreshold,
            decimal partialThreshold) =>
            ratePct >= fullThreshold ? LearningCoverage.Full
            : ratePct >= partialThreshold ? LearningCoverage.Partial
            : LearningCoverage.Low;
    }
}
