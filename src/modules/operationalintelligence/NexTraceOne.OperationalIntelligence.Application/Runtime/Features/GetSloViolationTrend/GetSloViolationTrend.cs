using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloViolationTrend;

/// <summary>
/// Feature: GetSloViolationTrend — tendência histórica de violações de SLO por janelas temporais.
///
/// Divide o período solicitado em janelas diárias e retorna o número de violações (Breached)
/// por janela, permitindo visualizar tendências de degradação de SLO ao longo do tempo.
///
/// Wave J.2 — SLO Tracking (OperationalIntelligence).
/// </summary>
public static class GetSloViolationTrend
{
    public sealed record Query(
        string TenantId,
        int Days = 30,
        string? ServiceName = null,
        string? MetricName = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Days).InclusiveBetween(1, 90);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.MetricName).MaximumLength(200).When(x => x.MetricName is not null);
        }
    }

    public sealed class Handler(
        ISloObservationRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var until = clock.UtcNow;
            var since = until.AddDays(-request.Days);

            var observations = await repository.ListByTenantAsync(
                request.TenantId, since, until, null, cancellationToken);

            // Filtrar por serviço e métrica se indicado
            if (request.ServiceName is not null)
                observations = observations
                    .Where(o => o.ServiceName.Equals(request.ServiceName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (request.MetricName is not null)
                observations = observations
                    .Where(o => o.MetricName.Equals(request.MetricName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            // Agrupar violações por dia (baseado em ObservedAt)
            var windows = Enumerable.Range(0, request.Days)
                .Select(offset =>
                {
                    var windowStart = since.Date.AddDays(offset);
                    var windowEnd = windowStart.AddDays(1);
                    var inWindow = observations
                        .Where(o => o.ObservedAt.UtcDateTime >= windowStart && o.ObservedAt.UtcDateTime < windowEnd)
                        .ToList();
                    return new ViolationWindow(
                        Date: windowStart,
                        TotalObservations: inWindow.Count,
                        Violations: inWindow.Count(o => o.Status == SloObservationStatus.Breached),
                        Warnings: inWindow.Count(o => o.Status == SloObservationStatus.Warning),
                        Met: inWindow.Count(o => o.Status == SloObservationStatus.Met));
                })
                .ToList();

            var totalViolations = windows.Sum(w => w.Violations);
            var peakViolationDay = windows.OrderByDescending(w => w.Violations).First();
            var trend = DetermineTrend(windows);

            return Result<Response>.Success(new Response(
                GeneratedAt: until,
                PeriodDays: request.Days,
                TenantId: request.TenantId,
                ServiceFilter: request.ServiceName,
                MetricFilter: request.MetricName,
                TotalViolations: totalViolations,
                Trend: trend,
                PeakViolationDate: peakViolationDay.Violations > 0 ? peakViolationDay.Date : null,
                Windows: windows));
        }

        private static string DetermineTrend(IReadOnlyList<ViolationWindow> windows)
        {
            if (windows.Count < 4) return "Insufficient";

            var half = windows.Count / 2;
            var recentAvg = windows.Skip(half).Average(w => w.Violations);
            var olderAvg = windows.Take(half).Average(w => w.Violations);

            if (recentAvg > olderAvg * 1.2) return "Worsening";
            if (recentAvg < olderAvg * 0.8) return "Improving";
            return "Stable";
        }
    }

    public sealed record ViolationWindow(
        DateTime Date,
        int TotalObservations,
        int Violations,
        int Warnings,
        int Met);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string TenantId,
        string? ServiceFilter,
        string? MetricFilter,
        int TotalViolations,
        string Trend,
        DateTime? PeakViolationDate,
        IReadOnlyList<ViolationWindow> Windows);
}
