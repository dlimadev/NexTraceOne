using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetSloComplianceSummary;

/// <summary>
/// Feature: GetSloComplianceSummary — relatório de conformidade de SLO por serviço.
///
/// Agrega observações de SLO num período e retorna:
/// - Taxa de conformidade por serviço e métrica (% de observações Met)
/// - Número de violações (Breached) e alertas (Warning)
/// - Conformidade geral (All Met / Partial / Violated)
///
/// Wave J.2 — SLO Tracking (OperationalIntelligence).
/// </summary>
public static class GetSloComplianceSummary
{
    public sealed record Query(
        string TenantId,
        int Days = 30,
        string? ServiceName = null,
        string? Environment = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Days).InclusiveBetween(1, 365);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.Environment).MaximumLength(100).When(x => x.Environment is not null);
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

            IReadOnlyList<Domain.Runtime.Entities.SloObservation> observations;
            if (request.ServiceName is not null)
            {
                observations = await repository.ListByServiceAsync(
                    request.TenantId, request.ServiceName, since, until,
                    request.Environment, cancellationToken);
            }
            else
            {
                observations = await repository.ListByTenantAsync(
                    request.TenantId, since, until, null, cancellationToken);
                if (request.Environment is not null)
                    observations = observations
                        .Where(o => o.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            // Agrupar por serviço + métrica
            var groups = observations
                .GroupBy(o => new { o.ServiceName, o.Environment, o.MetricName })
                .Select(g =>
                {
                    var total = g.Count();
                    var metCount = g.Count(o => o.Status == SloObservationStatus.Met);
                    var warningCount = g.Count(o => o.Status == SloObservationStatus.Warning);
                    var breachedCount = g.Count(o => o.Status == SloObservationStatus.Breached);
                    var complianceRate = total > 0 ? Math.Round((decimal)metCount / total * 100, 2) : 100m;

                    return new ServiceSloSummary(
                        ServiceName: g.Key.ServiceName,
                        Environment: g.Key.Environment,
                        MetricName: g.Key.MetricName,
                        TotalObservations: total,
                        MetCount: metCount,
                        WarningCount: warningCount,
                        BreachedCount: breachedCount,
                        ComplianceRate: complianceRate,
                        LastObservedValue: g.OrderByDescending(o => o.ObservedAt).First().ObservedValue,
                        SloTarget: g.First().SloTarget,
                        Unit: g.First().Unit);
                })
                .OrderBy(s => s.ComplianceRate)
                .ToList();

            var totalViolations = groups.Sum(g => g.BreachedCount);
            var overallStatus = groups.Count == 0
                ? "NoData"
                : totalViolations == 0
                    ? "AllMet"
                    : groups.Any(g => g.ComplianceRate < 80)
                        ? "Violated"
                        : "Partial";

            return Result<Response>.Success(new Response(
                GeneratedAt: until,
                PeriodDays: request.Days,
                TenantId: request.TenantId,
                ServiceFilter: request.ServiceName,
                EnvironmentFilter: request.Environment,
                OverallStatus: overallStatus,
                TotalObservations: observations.Count,
                TotalViolations: totalViolations,
                ServiceSummaries: groups));
        }
    }

    public sealed record ServiceSloSummary(
        string ServiceName,
        string Environment,
        string MetricName,
        int TotalObservations,
        int MetCount,
        int WarningCount,
        int BreachedCount,
        decimal ComplianceRate,
        decimal LastObservedValue,
        decimal SloTarget,
        string Unit);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string TenantId,
        string? ServiceFilter,
        string? EnvironmentFilter,
        string OverallStatus,
        int TotalObservations,
        int TotalViolations,
        IReadOnlyList<ServiceSloSummary> ServiceSummaries);
}
