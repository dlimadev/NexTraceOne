using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetIncidentChangeCorrelationReport;

/// <summary>
/// Feature: GetIncidentChangeCorrelationReport — correlação entre releases e incidentes pós-deploy.
///
/// Analisa releases no período e identifica, para cada release, se existem eventos
/// do tipo "incident_correlated" registados na timeline de mudança. Agrupa por serviço
/// e classifica o risco de incidente pós-deploy em:
/// - <c>Low</c> — taxa de incidente &lt; 5%
/// - <c>Medium</c> — taxa de incidente 5–15%
/// - <c>High</c> — taxa de incidente 15–30%
/// - <c>Critical</c> — taxa de incidente &gt; 30%
///
/// Produz:
/// - totais de releases no período e releases com incidente correlacionado
/// - taxa global de incidente pós-deploy no tenant
/// - distribuição de risk tier por serviço
/// - top serviços com maior taxa de correlação incidente/deploy
/// - top serviços com maior contagem absoluta de incidentes pós-deploy
///
/// Permite que Architect e Tech Lead identifiquem padrões de instabilidade após deploys
/// e priorizem ações de estabilização ou reforço de gates de promoção.
///
/// Wave R.1 — Incident-Change Correlation Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetIncidentChangeCorrelationReport
{
    private const string IncidentCorrelatedEventType = "incident_correlated";

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>TopServicesCount</c>: número máximo de serviços no ranking (1–100, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int TopServicesCount = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de risco de incidente pós-deploy para um serviço.</summary>
    public enum IncidentCorrelationRisk
    {
        /// <summary>Taxa de incidente &lt; 5% — risco baixo.</summary>
        Low,
        /// <summary>Taxa de incidente 5–15% — risco médio.</summary>
        Medium,
        /// <summary>Taxa de incidente 15–30% — risco elevado.</summary>
        High,
        /// <summary>Taxa de incidente &gt; 30% — risco crítico, requer atenção imediata.</summary>
        Critical
    }

    /// <summary>Distribuição de serviços por nível de risco.</summary>
    public sealed record RiskTierDistribution(
        int LowCount,
        int MediumCount,
        int HighCount,
        int CriticalCount);

    /// <summary>Métricas de correlação incidente/deploy de um serviço.</summary>
    public sealed record ServiceCorrelationEntry(
        string ServiceName,
        int TotalReleases,
        int ReleasesWithIncident,
        decimal IncidentRatePct,
        IncidentCorrelationRisk RiskTier);

    /// <summary>Resultado do relatório de correlação incidente-mudança.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalReleasesInPeriod,
        int ReleasesWithCorrelatedIncident,
        decimal TenantIncidentRatePct,
        int DistinctServicesWithIncident,
        RiskTierDistribution RiskDistribution,
        IReadOnlyList<ServiceCorrelationEntry> TopServicesByIncidentRate,
        IReadOnlyList<ServiceCorrelationEntry> TopServicesByAbsoluteIncidentCount);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.TopServicesCount).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    private const decimal MediumRiskThresholdPct = 5m;
    private const decimal HighRiskThresholdPct = 15m;
    private const decimal CriticalRiskThresholdPct = 30m;

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IChangeEventRepository _changeEventRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IChangeEventRepository changeEventRepo,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _changeEventRepo = Guard.Against.Null(changeEventRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);
            var tenantId = Guid.Parse(query.TenantId);

            var releases = await _releaseRepo.ListInRangeAsync(from, now, query.Environment, tenantId, cancellationToken);

            // Aggregate per service: total releases and incident-correlated releases
            var serviceMap = new Dictionary<string, (int Total, int WithIncident)>(StringComparer.OrdinalIgnoreCase);

            int totalWithIncident = 0;

            foreach (var release in releases)
            {
                if (!serviceMap.TryGetValue(release.ServiceName, out var counts))
                    counts = (0, 0);

                var incidentEvents = await _changeEventRepo.ListByReleaseIdAndEventTypeAsync(
                    release.Id, IncidentCorrelatedEventType, cancellationToken);

                bool hasIncident = incidentEvents.Count > 0;
                if (hasIncident) totalWithIncident++;

                serviceMap[release.ServiceName] = (counts.Total + 1, counts.WithIncident + (hasIncident ? 1 : 0));
            }

            // Build per-service entries
            var entries = serviceMap
                .Select(kvp =>
                {
                    var rate = kvp.Value.Total > 0
                        ? Math.Round((decimal)kvp.Value.WithIncident / kvp.Value.Total * 100m, 2)
                        : 0m;
                    return new ServiceCorrelationEntry(
                        ServiceName: kvp.Key,
                        TotalReleases: kvp.Value.Total,
                        ReleasesWithIncident: kvp.Value.WithIncident,
                        IncidentRatePct: rate,
                        RiskTier: ClassifyRisk(rate));
                })
                .ToList();

            int lowCount = entries.Count(e => e.RiskTier == IncidentCorrelationRisk.Low);
            int mediumCount = entries.Count(e => e.RiskTier == IncidentCorrelationRisk.Medium);
            int highCount = entries.Count(e => e.RiskTier == IncidentCorrelationRisk.High);
            int criticalCount = entries.Count(e => e.RiskTier == IncidentCorrelationRisk.Critical);

            int distinctServicesWithIncident = entries.Count(e => e.ReleasesWithIncident > 0);

            decimal tenantIncidentRate = releases.Count > 0
                ? Math.Round((decimal)totalWithIncident / releases.Count * 100m, 2)
                : 0m;

            var topByRate = entries
                .OrderByDescending(e => e.IncidentRatePct)
                .Take(query.TopServicesCount)
                .ToList();

            var topByAbsoluteCount = entries
                .OrderByDescending(e => e.ReleasesWithIncident)
                .Take(query.TopServicesCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalReleasesInPeriod: releases.Count,
                ReleasesWithCorrelatedIncident: totalWithIncident,
                TenantIncidentRatePct: tenantIncidentRate,
                DistinctServicesWithIncident: distinctServicesWithIncident,
                RiskDistribution: new RiskTierDistribution(lowCount, mediumCount, highCount, criticalCount),
                TopServicesByIncidentRate: topByRate,
                TopServicesByAbsoluteIncidentCount: topByAbsoluteCount));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static IncidentCorrelationRisk ClassifyRisk(decimal ratePct) => ratePct switch
        {
            >= CriticalRiskThresholdPct => IncidentCorrelationRisk.Critical,
            >= HighRiskThresholdPct => IncidentCorrelationRisk.High,
            >= MediumRiskThresholdPct => IncidentCorrelationRisk.Medium,
            _ => IncidentCorrelationRisk.Low
        };
    }
}
