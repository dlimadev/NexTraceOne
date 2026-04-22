using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetUncatalogedServicesReport;

/// <summary>
/// Feature: GetUncatalogedServicesReport — detecção de shadow services activos em telemetria sem registo no catálogo.
///
/// Cruza serviços observados em telemetria com <c>ServiceAsset</c> registado para detectar
/// serviços que existem em produção mas são invisíveis para governança.
///
/// Métricas:
/// - <c>UncatalogedCount</c> — total de serviços sem registo
/// - <c>ShadowServiceRisk</c> — % de serviços activos sem governança
/// - <c>CatalogCoverageRate</c> — 100 - ShadowServiceRisk
/// - <c>SuggestedTier</c> — inferido por volume: Alto→Critical, Médio→Standard, Baixo→Internal
/// - <c>QuickRegisterList</c> — lista pré-preenchida para registo rápido
///
/// Wave AM.1 — Auto-Cataloging &amp; Service Discovery Intelligence (Catalog Services).
/// </summary>
public static class GetUncatalogedServicesReport
{
    internal const int DefaultLookbackDays = 30;
    internal const int DefaultMinDailyCalls = 10;

    // ── Tier thresholds by daily call count ────────────────────────────────
    private const int CriticalTierThreshold = 1000;
    private const int StandardTierThreshold = 100;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int MinDailyCalls = DefaultMinDailyCalls) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.MinDailyCalls).GreaterThanOrEqualTo(0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum SuggestedServiceTier { Critical, Standard, Internal }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record UncatalogedServiceRow(
        string ServiceName,
        DateTimeOffset FirstSeen,
        DateTimeOffset LastSeen,
        int DailyCallCount,
        IReadOnlyList<string> ObservedEnvironments,
        string? PossibleOwner,
        SuggestedServiceTier SuggestedTier);

    public sealed record QuickRegisterEntry(
        string ServiceName,
        SuggestedServiceTier SuggestedTier,
        string? SuggestedOwner,
        IReadOnlyList<string> ObservedEnvironments);

    public sealed record Report(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int CatalogedServiceCount,
        int UncatalogedCount,
        decimal ShadowServiceRisk,
        decimal CatalogCoverageRate,
        IReadOnlyList<UncatalogedServiceRow> UncatalogedServices,
        IReadOnlyList<QuickRegisterEntry> QuickRegisterList,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IUncatalogedServicesReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.LookbackDays);

            var summary = await reader.GetSummaryAsync(request.TenantId, request.LookbackDays, cancellationToken);

            var significantServices = summary.UncatalogedServices
                .Where(s => s.DailyCallCount >= request.MinDailyCalls)
                .OrderByDescending(s => s.DailyCallCount)
                .ToList();

            var rows = significantServices.Select(s =>
            {
                var tier = s.DailyCallCount >= CriticalTierThreshold ? SuggestedServiceTier.Critical
                    : s.DailyCallCount >= StandardTierThreshold ? SuggestedServiceTier.Standard
                    : SuggestedServiceTier.Internal;
                return new UncatalogedServiceRow(
                    s.ServiceName, s.FirstSeen, s.LastSeen, s.DailyCallCount,
                    s.ObservedEnvironments, s.PossibleOwner, tier);
            }).ToList();

            var totalActive = summary.CatalogedServiceCount + rows.Count;
            var shadowRisk = totalActive == 0 ? 0m
                : Math.Round((decimal)rows.Count / totalActive * 100m, 2);
            var coverageRate = Math.Round(100m - shadowRisk, 2);

            var quickRegister = rows.Select(r => new QuickRegisterEntry(
                r.ServiceName, r.SuggestedTier, r.PossibleOwner, r.ObservedEnvironments)).ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, since, now,
                summary.CatalogedServiceCount, rows.Count,
                shadowRisk, coverageRate, rows, quickRegister, now));
        }
    }
}
