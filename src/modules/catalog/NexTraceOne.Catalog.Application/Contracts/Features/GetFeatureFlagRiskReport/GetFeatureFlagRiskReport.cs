using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using static NexTraceOne.Catalog.Application.Contracts.Abstractions.IFeatureFlagRiskReader;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagRiskReport;

/// <summary>
/// Feature: GetFeatureFlagRiskReport — risco consolidado das feature flags do tenant.
///
/// Calcula <c>FlagRiskScore</c> (0–100) por flag:
/// - Staleness  (30%) — StalenessRisk.High=100 / Medium=50 / Low=0
/// - Ownership  (25%) — OwnershipRisk.None=100 / Low=0
/// - ProdPresence (30%) — ProductionPresenceRisk.High=100 / Medium=50 / Low=0
/// - IncidentCorrelation (15%) — true=100 / false=0
///
/// <c>FlagRiskTier</c>: Safe ≤25 / Monitor ≤55 / Review ≤80 / Urgent >80
///
/// Wave AS.2 — Feature Flag &amp; Experimentation Governance (Catalog Contracts).
/// </summary>
public static class GetFeatureFlagRiskReport
{
    // ── Weights ────────────────────────────────────────────────────────────
    private const decimal StalenessWeight   = 0.30m;
    private const decimal OwnershipWeight   = 0.25m;
    private const decimal ProdPresenceWeight = 0.30m;
    private const decimal IncidentWeight    = 0.15m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal UrgentThreshold  = 80m;
    private const decimal ReviewThreshold  = 55m;
    private const decimal MonitorThreshold = 25m;

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de risco de feature flags.</summary>
    public sealed record Query(
        string TenantId,
        int StaleFlagDays = 60,
        int ProdPresenceDays = 90,
        int IncidentWindowHours = 24) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.StaleFlagDays).InclusiveBetween(1, 730);
            RuleFor(x => x.ProdPresenceDays).InclusiveBetween(1, 730);
            RuleFor(x => x.IncidentWindowHours).InclusiveBetween(1, 8760);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Tier de risco de uma flag individual.</summary>
    public enum FlagRiskTier { Safe, Monitor, Review, Urgent }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Linha de risco por flag.</summary>
    public sealed record FlagRiskRow(
        string ServiceId,
        string ServiceName,
        string FlagKey,
        StalenessRisk StalenessRisk,
        OwnershipRisk OwnershipRisk,
        ProductionPresenceRisk ProductionPresenceRisk,
        bool IncidentCorrelated,
        decimal FlagRiskScore,
        FlagRiskTier FlagRiskTier);

    /// <summary>Sumário global de risco de flags do tenant.</summary>
    public sealed record TenantFlagRiskSummary(
        int UrgentFlagCount,
        int ReviewFlagCount,
        decimal TenantFlagRiskIndex);

    /// <summary>Relatório completo de risco de feature flags.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<FlagRiskRow> ByFlag,
        TenantFlagRiskSummary Summary,
        IReadOnlyList<string> ScheduledRemovalOverdue,
        IReadOnlyList<string> ToggleWithIncidentCorrelation,
        IReadOnlyList<string> RecommendedRemovals,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IFeatureFlagRiskReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListFlagRiskByTenantAsync(
                request.TenantId,
                request.StaleFlagDays,
                request.ProdPresenceDays,
                request.IncidentWindowHours,
                cancellationToken);

            var rows = entries.Select(e =>
            {
                var stalenessScore = e.StalenessRisk switch
                {
                    StalenessRisk.High   => 100m,
                    StalenessRisk.Medium => 50m,
                    _                   => 0m
                };
                var ownershipScore = e.OwnershipRisk == OwnershipRisk.None ? 100m : 0m;
                var prodScore = e.ProductionPresenceRisk switch
                {
                    ProductionPresenceRisk.High   => 100m,
                    ProductionPresenceRisk.Medium => 50m,
                    _                            => 0m
                };
                var incidentScore = e.IncidentCorrelated ? 100m : 0m;

                var riskScore = Math.Round(
                    stalenessScore   * StalenessWeight +
                    ownershipScore   * OwnershipWeight +
                    prodScore        * ProdPresenceWeight +
                    incidentScore    * IncidentWeight, 2);

                var tier = riskScore > UrgentThreshold  ? FlagRiskTier.Urgent
                    : riskScore > ReviewThreshold  ? FlagRiskTier.Review
                    : riskScore > MonitorThreshold ? FlagRiskTier.Monitor
                    : FlagRiskTier.Safe;

                return new FlagRiskRow(
                    e.ServiceId, e.ServiceName, e.FlagKey,
                    e.StalenessRisk, e.OwnershipRisk, e.ProductionPresenceRisk,
                    e.IncidentCorrelated, riskScore, tier);
            }).ToList();

            var urgentCount  = rows.Count(r => r.FlagRiskTier == FlagRiskTier.Urgent);
            var reviewCount  = rows.Count(r => r.FlagRiskTier == FlagRiskTier.Review);
            var indexPct     = rows.Count == 0 ? 100m
                : Math.Round((decimal)rows.Count(r =>
                    r.FlagRiskTier is FlagRiskTier.Safe or FlagRiskTier.Monitor)
                  / rows.Count * 100m, 2);

            var summary = new TenantFlagRiskSummary(urgentCount, reviewCount, indexPct);

            // Flags com remoção agendada vencida
            var overdueRemovals = entries
                .Where(e => e.ScheduledRemovalDate.HasValue && e.ScheduledRemovalDate.Value < now)
                .Select(e => e.FlagKey)
                .ToList();

            // Flags com correlação com incidentes
            var incidentCorrelated = rows
                .Where(r => r.IncidentCorrelated)
                .Select(r => r.FlagKey)
                .ToList();

            // Remoções recomendadas: Urgent e sem correlação com incidentes
            var recommended = rows
                .Where(r => r.FlagRiskTier == FlagRiskTier.Urgent && !r.IncidentCorrelated)
                .Select(r => r.FlagKey)
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, rows, summary,
                overdueRemovals, incidentCorrelated, recommended, now));
        }
    }
}
