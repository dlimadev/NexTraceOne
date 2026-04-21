using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.FinOps.Features.GetFinOpsTrendReport;

/// <summary>
/// Feature: GetFinOpsTrendReport — relatório de tendência de custo operacional ao longo do tempo.
///
/// Agrega registos de custo do tenant e produz:
/// - custo total do período e custo do período anterior para calcular delta%
/// - série temporal de custo agregado por dia (DailySeries)
/// - distribuição de custo por categoria (Compute, Storage, Network, …)
/// - ranking dos serviços mais dispendiosos no período
///
/// Contextualizado por tenant, ambiente e categoria.
/// Adequado para Executive, Platform Admin e FinOps persona views.
///
/// Wave O.2 — FinOps Trend Report (OperationalIntelligence).
/// </summary>
public static class GetFinOpsTrendReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant.</para>
    /// <para><c>LookbackDays</c>: período atual de análise (1–365, default 30).</para>
    /// <para><c>TopServicesCount</c>: número máximo de serviços dispendiosos a listar (1–50, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional por ambiente.</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int TopServicesCount = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Ponto da série temporal de custo diário.</summary>
    public sealed record DailyCostPoint(DateOnly Date, decimal TotalUsd);

    /// <summary>Custo por categoria no período.</summary>
    public sealed record CategoryCostEntry(string Category, decimal TotalUsd, decimal Percent);

    /// <summary>Serviço mais dispendioso no período.</summary>
    public sealed record TopServiceCostEntry(string ServiceName, string? TeamId, decimal TotalUsd, decimal Percent);

    /// <summary>Relatório de tendência de custo operacional.</summary>
    public sealed record Report(
        string TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        int LookbackDays,
        decimal CurrentPeriodTotalUsd,
        decimal PreviousPeriodTotalUsd,
        decimal DeltaPercent,
        IReadOnlyList<DailyCostPoint> DailySeries,
        IReadOnlyList<CategoryCostEntry> ByCategory,
        IReadOnlyList<TopServiceCostEntry> TopServices,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.TopServicesCount).InclusiveBetween(1, 50);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IServiceCostAllocationRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var currentFrom = now.AddDays(-request.LookbackDays);
            var previousFrom = currentFrom.AddDays(-request.LookbackDays);

            // Current and previous period records
            var currentRecords = await repository.ListByTenantAsync(
                request.TenantId, currentFrom, now,
                request.Environment, null, cancellationToken);

            var previousRecords = await repository.ListByTenantAsync(
                request.TenantId, previousFrom, currentFrom,
                request.Environment, null, cancellationToken);

            var currentTotal = currentRecords.Sum(r => r.AmountUsd);
            var previousTotal = previousRecords.Sum(r => r.AmountUsd);

            var deltaPercent = previousTotal == 0
                ? (currentTotal > 0 ? 100m : 0m)
                : Math.Round((currentTotal - previousTotal) / previousTotal * 100m, 1);

            // Daily series: group by day of PeriodStart
            var dailySeries = currentRecords
                .GroupBy(r => DateOnly.FromDateTime(r.PeriodStart.UtcDateTime))
                .OrderBy(g => g.Key)
                .Select(g => new DailyCostPoint(g.Key, g.Sum(r => r.AmountUsd)))
                .ToList();

            // Category breakdown
            var byCategory = currentRecords
                .GroupBy(r => r.Category)
                .OrderByDescending(g => g.Sum(r => r.AmountUsd))
                .Select(g =>
                {
                    var cat = g.Sum(r => r.AmountUsd);
                    return new CategoryCostEntry(
                        g.Key.ToString(),
                        cat,
                        currentTotal == 0 ? 0m : Math.Round(cat * 100m / currentTotal, 1));
                })
                .ToList();

            // Top services
            var topServices = currentRecords
                .GroupBy(r => r.ServiceName)
                .Select(g => (
                    ServiceName: g.Key,
                    TeamId: g.First().TeamId,
                    Total: g.Sum(r => r.AmountUsd)))
                .OrderByDescending(x => x.Total)
                .Take(request.TopServicesCount)
                .Select(x => new TopServiceCostEntry(
                    x.ServiceName,
                    x.TeamId,
                    x.Total,
                    currentTotal == 0 ? 0m : Math.Round(x.Total * 100m / currentTotal, 1)))
                .ToList();

            var report = new Report(
                TenantId: request.TenantId,
                From: currentFrom,
                To: now,
                LookbackDays: request.LookbackDays,
                CurrentPeriodTotalUsd: currentTotal,
                PreviousPeriodTotalUsd: previousTotal,
                DeltaPercent: deltaPercent,
                DailySeries: dailySeries,
                ByCategory: byCategory,
                TopServices: topServices,
                GeneratedAt: now);

            return Result<Report>.Success(report);
        }
    }
}
