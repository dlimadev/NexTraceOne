using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetErrorBudgetReport;

/// <summary>
/// Feature: GetErrorBudgetReport — tracking de consumo de error budget por serviço por janela de SLO.
///
/// Para cada serviço com SLO activo:
/// - <c>ErrorBudgetPct</c>      — (1 - SloTarget) × 100 (budget total disponível no período)
/// - <c>BudgetConsumedPct</c>   — (1 - ActualCompliance) / ErrorBudgetPct × 100
/// - <c>BudgetRemainingPct</c>  — 100 - BudgetConsumedPct
/// - <c>BurnRate</c>            — taxa actual vs. ideal (ActualBurnRate / IdealBurnRate)
/// - <c>DaysToExhaustion</c>    — projecção linear (∞ se BurnRate ≤ 1.0)
///
/// <c>ErrorBudgetTier</c>:
/// - <c>Healthy</c>    — ≥ HealthyRemainingPct remaining
/// - <c>Warning</c>    — ≥ WarningRemainingPct remaining
/// - <c>Exhausted</c>  — 0% remaining (BudgetRemainingPct = 0)
/// - <c>Burned</c>     — negativo (em deficit: BudgetRemainingPct &lt; 0)
///
/// Wave AN.1 — SRE Intelligence &amp; Error Budget Management (OperationalIntelligence Runtime).
/// </summary>
public static class GetErrorBudgetReport
{
    internal const decimal DefaultHealthyRemainingPct = 70m;
    internal const decimal DefaultWarningRemainingPct = 30m;
    internal const int DefaultPeriodDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int PeriodDays = DefaultPeriodDays,
        decimal HealthyRemainingPct = DefaultHealthyRemainingPct,
        decimal WarningRemainingPct = DefaultWarningRemainingPct) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PeriodDays).InclusiveBetween(1, 90);
            RuleFor(x => x.HealthyRemainingPct).InclusiveBetween(0m, 100m);
            RuleFor(x => x.WarningRemainingPct).InclusiveBetween(0m, 100m);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum ErrorBudgetTier { Healthy, Warning, Exhausted, Burned }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record DailyBudgetPoint(DateTimeOffset Date, decimal BudgetConsumedPct);

    public sealed record ServiceErrorBudgetRow(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        decimal SloTargetPct,
        decimal ActualCompliancePct,
        decimal ErrorBudgetPct,
        decimal BudgetConsumedPct,
        decimal BudgetRemainingPct,
        decimal BurnRate,
        decimal? DaysToExhaustion,
        DateTimeOffset BudgetPeriodEndDate,
        ErrorBudgetTier Tier,
        IReadOnlyList<DailyBudgetPoint> Timeline);

    public sealed record TenantBudgetHealthSummary(
        int HealthyServices,
        int WarningServices,
        int ExhaustedServices,
        int BurnedServices,
        decimal GlobalBurnRate,
        IReadOnlyList<ServiceErrorBudgetRow> TopBurningServices);

    public sealed record Report(
        string TenantId,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        int TotalServicesAnalyzed,
        TenantBudgetHealthSummary Summary,
        IReadOnlyList<ServiceErrorBudgetRow> ByService,
        IReadOnlyList<ServiceErrorBudgetRow> FreezeRecommendations,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IErrorBudgetReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.PeriodDays);

            var entries = await reader.ListByTenantAsync(request.TenantId, request.PeriodDays, cancellationToken);

            var rows = entries.Select(e =>
            {
                // Error budget = allowed failure budget in the period
                var errorBudgetPct = Math.Round((1m - e.SloTargetPct / 100m) * 100m, 4);
                if (errorBudgetPct <= 0m) errorBudgetPct = 0.001m; // guard division by zero

                var actualFailurePct = Math.Round((1m - e.ActualCompliancePct / 100m) * 100m, 4);
                var budgetConsumedPct = Math.Round(actualFailurePct / errorBudgetPct * 100m, 2);
                var budgetRemainingPct = Math.Round(100m - budgetConsumedPct, 2);

                // BurnRate: actual failure rate / ideal failure rate
                // Ideal: uniform consumption over period → daily ideal = errorBudgetPct / periodDays
                // We use overall ratio: actualFailurePct / errorBudgetPct = budgetConsumedPct / 100
                var burnRate = Math.Round(budgetConsumedPct / 100m, 4);

                // DaysToExhaustion: if consuming budget faster than 1x, project when it hits 100%
                decimal? daysToExhaustion = null;
                if (burnRate > 0m && budgetRemainingPct > 0m)
                {
                    // daily burn rate = burnRate / periodDays of observation
                    var dailyBurnRate = burnRate / request.PeriodDays;
                    if (dailyBurnRate > 0m)
                        daysToExhaustion = Math.Round(budgetRemainingPct / 100m / dailyBurnRate, 1);
                }

                var tier = budgetRemainingPct < 0m ? ErrorBudgetTier.Burned
                    : budgetRemainingPct == 0m ? ErrorBudgetTier.Exhausted
                    : budgetRemainingPct >= request.HealthyRemainingPct ? ErrorBudgetTier.Healthy
                    : budgetRemainingPct >= request.WarningRemainingPct ? ErrorBudgetTier.Warning
                    : ErrorBudgetTier.Exhausted;

                // Build daily timeline from samples
                var timeline = e.DailySamples.Select(s =>
                {
                    var dayFailure = Math.Round((1m - s.CompliancePct / 100m) * 100m, 4);
                    var dayConsumed = Math.Round(dayFailure / errorBudgetPct * 100m, 2);
                    return new DailyBudgetPoint(s.Date, dayConsumed);
                }).ToList();

                return new ServiceErrorBudgetRow(
                    e.ServiceId, e.ServiceName, e.TeamName, e.ServiceTier,
                    e.SloTargetPct, e.ActualCompliancePct,
                    errorBudgetPct, budgetConsumedPct, budgetRemainingPct,
                    burnRate, daysToExhaustion, e.PeriodEndDate, tier, timeline);
            }).ToList();

            var healthy = rows.Count(r => r.Tier == ErrorBudgetTier.Healthy);
            var warning = rows.Count(r => r.Tier == ErrorBudgetTier.Warning);
            var exhausted = rows.Count(r => r.Tier == ErrorBudgetTier.Exhausted);
            var burned = rows.Count(r => r.Tier == ErrorBudgetTier.Burned);

            var globalBurnRate = rows.Count == 0 ? 0m
                : Math.Round(rows.Average(r => r.BurnRate), 4);

            var topBurning = rows.OrderByDescending(r => r.BurnRate).Take(5).ToList();

            var freezeRecos = rows
                .Where(r => r.Tier is ErrorBudgetTier.Exhausted or ErrorBudgetTier.Burned)
                .OrderBy(r => r.BudgetRemainingPct)
                .ToList();

            var summary = new TenantBudgetHealthSummary(
                healthy, warning, exhausted, burned, globalBurnRate, topBurning);

            return Result<Report>.Success(new Report(
                request.TenantId, since, now,
                rows.Count, summary, rows, freezeRecos, now));
        }
    }
}
