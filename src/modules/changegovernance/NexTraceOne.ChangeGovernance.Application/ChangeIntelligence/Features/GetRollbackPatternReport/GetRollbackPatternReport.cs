using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRollbackPatternReport;

/// <summary>
/// Feature: GetRollbackPatternReport — análise de padrões de rollback por serviço.
///
/// Para cada serviço com releases revertidas no período analisa:
/// - <c>TotalRollbacks</c> e <c>RollbackRate</c> (rollbacks / total releases)
/// - <c>AvgConfidenceAtRollback</c> — score médio de confiança das releases revertidas
/// - <c>AvgEvidencePackCompleteness</c> — completude média dos evidence packs nas releases revertidas
///
/// Classifica por <c>RollbackPattern</c>:
/// - <c>None</c>      — 0 rollbacks no período
/// - <c>Isolated</c>  — 1 rollback (one-off)
/// - <c>Recurring</c> — 2–3 rollbacks (padrão de atenção)
/// - <c>Serial</c>    — ≥ 4 rollbacks (disfunção sistémica)
///
/// Sinaliza:
/// - <c>SystemicRisk</c> — Serial com AvgConfidenceAtRollback &lt; 50
/// - <c>EvidenceGap</c>  — rollbacks com AvgEvidencePackCompleteness &lt; 70%
///
/// Produz distribuição global por RollbackPattern, top serviços com maior RollbackRate
/// e top serviços Serial no tenant.
///
/// Orienta Tech Lead, Architect e Platform Admin no loop entre change quality e rollback intelligence.
///
/// Wave W.1 — Rollback Pattern Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetRollbackPatternReport
{
    private const int SerialThreshold = 4;
    private const decimal SystemicRiskConfidenceThreshold = 50m;
    private const decimal EvidenceGapCompletenessThreshold = 70m;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (7–180, default 90).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking (1–100, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        Guid TenantId,
        int LookbackDays = 90,
        int MaxTopServices = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Padrão de rollback de um serviço no período.</summary>
    public enum RollbackPattern
    {
        /// <summary>Sem rollbacks no período — historial limpo.</summary>
        None,
        /// <summary>1 rollback no período — ocorrência isolada.</summary>
        Isolated,
        /// <summary>2–3 rollbacks no período — padrão de atenção.</summary>
        Recurring,
        /// <summary>≥ 4 rollbacks no período — disfunção sistémica.</summary>
        Serial
    }

    /// <summary>Distribuição global de serviços por RollbackPattern.</summary>
    public sealed record PatternDistribution(
        int NoneCount,
        int IsolatedCount,
        int RecurringCount,
        int SerialCount);

    /// <summary>Entrada de padrão de rollback de um serviço.</summary>
    public sealed record ServiceRollbackEntry(
        string ServiceName,
        int TotalReleases,
        int TotalRollbacks,
        decimal RollbackRatePct,
        decimal AvgConfidenceAtRollback,
        decimal AvgEvidencePackCompletenessPct,
        RollbackPattern Pattern,
        bool SystemicRisk,
        bool EvidenceGap);

    /// <summary>Resultado do relatório de padrões de rollback.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        int TotalReleasesInPeriod,
        int TotalRollbacksInPeriod,
        decimal GlobalRollbackRatePct,
        PatternDistribution Distribution,
        IReadOnlyList<ServiceRollbackEntry> TopByRollbackRate,
        IReadOnlyList<ServiceRollbackEntry> TopSerialServices,
        IReadOnlyList<ServiceRollbackEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 180);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IChangeConfidenceBreakdownRepository _confidenceRepo;
        private readonly IEvidencePackRepository _evidencePackRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IChangeConfidenceBreakdownRepository confidenceRepo,
            IEvidencePackRepository evidencePackRepo,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _confidenceRepo = Guard.Against.Null(confidenceRepo);
            _evidencePackRepo = Guard.Against.Null(evidencePackRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Default(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            // 1. Fetch all releases in the period
            var allReleases = await _releaseRepo.ListInRangeAsync(
                from, now, query.Environment, query.TenantId, cancellationToken);

            if (allReleases.Count == 0)
            {
                return Result<Report>.Success(EmptyReport(now, query.LookbackDays));
            }

            // 2. Identify rolled-back releases
            var rolledBack = allReleases
                .Where(r => r.Status == DeploymentStatus.RolledBack)
                .ToList();

            var rollbackReleaseIds = rolledBack.Select(r => r.Id).ToList();

            // 3. Batch-fetch confidence breakdowns and evidence packs
            Dictionary<ReleaseId, decimal> confidenceByRelease = [];
            Dictionary<Guid, decimal> completenessByRelease = [];

            if (rollbackReleaseIds.Count > 0)
            {
                var breakdowns = await _confidenceRepo.ListByReleaseIdsAsync(
                    rollbackReleaseIds, cancellationToken);
                foreach (var bd in breakdowns)
                    confidenceByRelease[bd.ReleaseId] = bd.AggregatedScore;

                var evidencePacks = await _evidencePackRepo.ListByReleaseIdsAsync(
                    rollbackReleaseIds.Select(id => id.Value), cancellationToken);
                foreach (var ep in evidencePacks)
                    completenessByRelease.TryAdd(ep.ReleaseId, ep.CompletenessPercentage);
            }

            // 4. Group all releases by service
            var byService = allReleases
                .GroupBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    StringComparer.OrdinalIgnoreCase);

            // 5. Build per-service entries
            var entries = new List<ServiceRollbackEntry>(byService.Count);

            foreach (var (serviceName, releases) in byService)
            {
                var svcRollbacks = releases
                    .Where(r => r.Status == DeploymentStatus.RolledBack)
                    .ToList();

                int totalReleases = releases.Count;
                int totalRollbacks = svcRollbacks.Count;

                decimal rollbackRate = totalReleases > 0
                    ? Math.Round((decimal)totalRollbacks / totalReleases * 100m, 1)
                    : 0m;

                // Avg confidence at rollback (default 0 when no breakdown recorded)
                var confScores = svcRollbacks
                    .Select(r => confidenceByRelease.GetValueOrDefault(r.Id, 0m))
                    .ToList();
                decimal avgConfidence = confScores.Count > 0
                    ? Math.Round(confScores.Average(), 1)
                    : 0m;

                // Avg evidence pack completeness (default 0 when no pack recorded)
                var compScores = svcRollbacks
                    .Select(r => completenessByRelease.GetValueOrDefault(r.Id.Value, 0m))
                    .ToList();
                decimal avgCompleteness = compScores.Count > 0
                    ? Math.Round(compScores.Average(), 1)
                    : 0m;

                var pattern = ClassifyPattern(totalRollbacks);
                bool systemicRisk = pattern == RollbackPattern.Serial
                    && avgConfidence < SystemicRiskConfidenceThreshold;
                bool evidenceGap = totalRollbacks > 0
                    && avgCompleteness < EvidenceGapCompletenessThreshold;

                entries.Add(new ServiceRollbackEntry(
                    ServiceName: serviceName,
                    TotalReleases: totalReleases,
                    TotalRollbacks: totalRollbacks,
                    RollbackRatePct: rollbackRate,
                    AvgConfidenceAtRollback: avgConfidence,
                    AvgEvidencePackCompletenessPct: avgCompleteness,
                    Pattern: pattern,
                    SystemicRisk: systemicRisk,
                    EvidenceGap: evidenceGap));
            }

            // 6. Aggregate
            int totalRollbacksGlobal = rolledBack.Count;
            decimal globalRollbackRate = allReleases.Count > 0
                ? Math.Round((decimal)totalRollbacksGlobal / allReleases.Count * 100m, 1)
                : 0m;

            var distribution = new PatternDistribution(
                NoneCount: entries.Count(e => e.Pattern == RollbackPattern.None),
                IsolatedCount: entries.Count(e => e.Pattern == RollbackPattern.Isolated),
                RecurringCount: entries.Count(e => e.Pattern == RollbackPattern.Recurring),
                SerialCount: entries.Count(e => e.Pattern == RollbackPattern.Serial));

            var topByRate = entries
                .Where(e => e.TotalRollbacks > 0)
                .OrderByDescending(e => e.RollbackRatePct)
                .ThenByDescending(e => e.TotalRollbacks)
                .Take(query.MaxTopServices)
                .ToList();

            var topSerial = entries
                .Where(e => e.Pattern == RollbackPattern.Serial)
                .OrderByDescending(e => e.TotalRollbacks)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxTopServices)
                .ToList();

            var allSorted = entries
                .OrderByDescending(e => e.TotalRollbacks)
                .ThenByDescending(e => e.RollbackRatePct)
                .ThenBy(e => e.ServiceName)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                TotalReleasesInPeriod: allReleases.Count,
                TotalRollbacksInPeriod: totalRollbacksGlobal,
                GlobalRollbackRatePct: globalRollbackRate,
                Distribution: distribution,
                TopByRollbackRate: topByRate,
                TopSerialServices: topSerial,
                AllServices: allSorted));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static RollbackPattern ClassifyPattern(int rollbackCount) => rollbackCount switch
        {
            0 => RollbackPattern.None,
            1 => RollbackPattern.Isolated,
            < SerialThreshold => RollbackPattern.Recurring,
            _ => RollbackPattern.Serial
        };

        private static Report EmptyReport(DateTimeOffset now, int lookbackDays) => new(
            GeneratedAt: now,
            LookbackDays: lookbackDays,
            TotalServicesAnalyzed: 0,
            TotalReleasesInPeriod: 0,
            TotalRollbacksInPeriod: 0,
            GlobalRollbackRatePct: 0m,
            Distribution: new PatternDistribution(0, 0, 0, 0),
            TopByRollbackRate: [],
            TopSerialServices: [],
            AllServices: []);
    }
}
