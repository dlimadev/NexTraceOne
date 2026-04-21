using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeWindowUtilizationReport;

/// <summary>
/// Feature: GetChangeWindowUtilizationReport — conformidade de janelas de mudança.
///
/// Analisa releases no período e verifica, para cada uma, se foi executada dentro de
/// uma janela de deployment activa (Scheduled) ou permitida para hotfixes (HotfixAllowed)
/// no Release Calendar. Deploys durante janelas Freeze ou Maintenance sem cobertura de
/// hotfix são classificados como fora de janela.
///
/// Classifica a conformidade de cada equipa em:
/// - <c>Excellent</c> — taxa de conformidade ≥ 95%
/// - <c>Good</c>      — taxa de conformidade ≥ 80%
/// - <c>AtRisk</c>    — taxa de conformidade &lt; 80%
///
/// Produz:
/// - totais de releases analisadas e fora de janela
/// - taxa global de conformidade no tenant
/// - número de equipas com calendário activo vs. sem calendário
/// - distribuição por tier de conformidade
/// - top equipas não-conformes (ordenadas por taxa de fora-de-janela)
///
/// Reforça o Release Calendar (Wave F.1) como mecanismo de governança de deployment
/// e dá visibilidade ao Platform Admin e Tech Lead sobre equipas que ignoram janelas.
///
/// Wave S.1 — Change Window Utilization Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetChangeWindowUtilizationReport
{
    // ── Limiares de tier ──────────────────────────────────────────────────
    private const decimal ExcellentThresholdPct = 95m;
    private const decimal GoodThresholdPct = 80m;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>TopNonCompliantCount</c>: número máximo de equipas não-conformes no ranking (1–50, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int TopNonCompliantCount = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de conformidade de janelas de mudança por equipa.</summary>
    public enum ConformanceTier
    {
        /// <summary>Taxa de conformidade ≥ 95% — excelente utilização das janelas.</summary>
        Excellent,
        /// <summary>Taxa de conformidade ≥ 80% — boa utilização, com margem de melhoria.</summary>
        Good,
        /// <summary>Taxa de conformidade &lt; 80% — equipa ignora frequentemente as janelas definidas.</summary>
        AtRisk
    }

    /// <summary>Distribuição de equipas por tier de conformidade.</summary>
    public sealed record ConformanceTierDistribution(
        int ExcellentCount,
        int GoodCount,
        int AtRiskCount);

    /// <summary>Métricas de conformidade de janelas de mudança de uma equipa.</summary>
    public sealed record TeamConformanceEntry(
        string TeamName,
        int TotalDeployments,
        int DeploymentsInWindow,
        int DeploymentsOutOfWindow,
        decimal ConformanceRatePct,
        ConformanceTier ConformanceTier,
        bool HasCalendarWindows);

    /// <summary>Resultado do relatório de utilização de janelas de mudança.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalReleasesAnalyzed,
        int TotalReleasesOutOfWindow,
        decimal TenantConformanceRatePct,
        int TeamsWithCalendar,
        int TeamsWithoutCalendar,
        ConformanceTierDistribution TierDistribution,
        IReadOnlyList<TeamConformanceEntry> TopNonCompliantTeams,
        IReadOnlyList<TeamConformanceEntry> AllTeams);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.TopNonCompliantCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IReleaseCalendarRepository _calendarRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IReleaseCalendarRepository calendarRepo,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _calendarRepo = Guard.Against.Null(calendarRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            var tenantId = Guid.Parse(query.TenantId);

            // Fetch releases and calendar windows in parallel
            var releasesTask = _releaseRepo.ListInRangeAsync(from, now, query.Environment, tenantId, cancellationToken);
            var windowsTask = _calendarRepo.ListAsync(
                query.TenantId,
                status: ReleaseWindowStatus.Active,
                windowType: null,
                from: from,
                to: now,
                ct: cancellationToken);

            await Task.WhenAll(releasesTask, windowsTask);

            var releases = releasesTask.Result;
            var windows = windowsTask.Result;

            // Only Scheduled and HotfixAllowed windows count as "allowed to deploy"
            var allowedWindows = windows
                .Where(w => w.WindowType is ReleaseWindowType.Scheduled or ReleaseWindowType.HotfixAllowed)
                .ToList();

            bool hasAnyCalendar = windows.Count > 0;

            // Group releases by team; fall back to service name when no team
            var teamGroups = releases
                .GroupBy(r => r.TeamName ?? r.ServiceName, StringComparer.OrdinalIgnoreCase);

            var entries = new List<TeamConformanceEntry>();
            int globalOutOfWindow = 0;

            foreach (var teamGroup in teamGroups)
            {
                string teamName = teamGroup.Key;
                int inWindow = 0;
                int outOfWindow = 0;

                foreach (var release in teamGroup)
                {
                    var releaseTime = release.CreatedAt;

                    // A release is "in window" if it falls within any allowed calendar window
                    bool withinWindow = allowedWindows.Any(w =>
                        releaseTime >= w.StartsAt && releaseTime <= w.EndsAt &&
                        (w.EnvironmentFilter is null ||
                         string.Equals(w.EnvironmentFilter, release.Environment, StringComparison.OrdinalIgnoreCase)));

                    if (withinWindow)
                        inWindow++;
                    else
                        outOfWindow++;
                }

                globalOutOfWindow += outOfWindow;

                int total = inWindow + outOfWindow;
                decimal conformanceRate = total > 0
                    ? Math.Round((decimal)inWindow / total * 100m, 2)
                    : 100m; // no deploys = trivially conformant

                entries.Add(new TeamConformanceEntry(
                    TeamName: teamName,
                    TotalDeployments: total,
                    DeploymentsInWindow: inWindow,
                    DeploymentsOutOfWindow: outOfWindow,
                    ConformanceRatePct: conformanceRate,
                    ConformanceTier: ClassifyTier(conformanceRate),
                    HasCalendarWindows: hasAnyCalendar));
            }

            int excellentCount = entries.Count(e => e.ConformanceTier == ConformanceTier.Excellent);
            int goodCount = entries.Count(e => e.ConformanceTier == ConformanceTier.Good);
            int atRiskCount = entries.Count(e => e.ConformanceTier == ConformanceTier.AtRisk);

            decimal tenantRate = releases.Count > 0
                ? Math.Round((decimal)(releases.Count - globalOutOfWindow) / releases.Count * 100m, 2)
                : 100m;

            int teamsWithCalendar = hasAnyCalendar ? entries.Count : 0;
            int teamsWithoutCalendar = hasAnyCalendar ? 0 : entries.Count;

            var topNonCompliant = entries
                .OrderBy(e => e.ConformanceRatePct)
                .Take(query.TopNonCompliantCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalReleasesAnalyzed: releases.Count,
                TotalReleasesOutOfWindow: globalOutOfWindow,
                TenantConformanceRatePct: tenantRate,
                TeamsWithCalendar: teamsWithCalendar,
                TeamsWithoutCalendar: teamsWithoutCalendar,
                TierDistribution: new ConformanceTierDistribution(excellentCount, goodCount, atRiskCount),
                TopNonCompliantTeams: topNonCompliant,
                AllTeams: entries.OrderBy(e => e.TeamName).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static ConformanceTier ClassifyTier(decimal ratePct) => ratePct switch
        {
            >= ExcellentThresholdPct => ConformanceTier.Excellent,
            >= GoodThresholdPct => ConformanceTier.Good,
            _ => ConformanceTier.AtRisk
        };
    }
}
