using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTopologyAwareAlerts;

/// <summary>
/// Feature: GetTopologyAwareAlerts — gera alertas inteligentes considerando o grafo de
/// dependências. Um serviço degradado que tem dependentes com alta utilização gera alerta
/// de propagação. Fundamenta topology-aware alerting sem telemetria externa.
/// </summary>
public static class GetTopologyAwareAlerts
{
    public sealed record Query(
        string ServiceId,
        string Environment,
        /// <summary>IDs dos serviços que dependem do ServiceId (downstream consumers).</summary>
        IReadOnlyList<string> DependentServiceIds,
        /// <summary>Threshold de taxa de erro (%) para disparar alerta. Padrão: 5%.</summary>
        decimal ErrorRateAlertThreshold = 5m,
        /// <summary>Threshold de latência (ms) para disparar alerta. Padrão: 500ms.</summary>
        decimal LatencyAlertThresholdMs = 500m) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DependentServiceIds).NotNull();
            RuleFor(x => x.ErrorRateAlertThreshold).InclusiveBetween(0m, 100m);
            RuleFor(x => x.LatencyAlertThresholdMs).GreaterThan(0m);
        }
    }

    public sealed class Handler(
        IRuntimeSnapshotRepository snapshotRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var windowStart = now - TimeSpan.FromHours(1);

            var alerts = new List<TopologyAlert>();

            // Avalia o serviço raiz
            var rootSnapshots = await snapshotRepository.ListByServiceAsync(
                request.ServiceId, request.Environment, 1, 60, cancellationToken);

            var recentRoot = rootSnapshots
                .Where(s => s.CapturedAt >= windowStart)
                .ToList();

            bool rootDegraded = false;
            if (recentRoot.Count > 0)
            {
                var avgLatency = recentRoot.Average(s => (double)s.AvgLatencyMs);
                var avgError = recentRoot.Average(s => (double)s.ErrorRate);

                if ((decimal)avgLatency > request.LatencyAlertThresholdMs)
                {
                    alerts.Add(new TopologyAlert(
                        ServiceId: request.ServiceId,
                        AlertType: "HighLatency",
                        Severity: "Warning",
                        Message: $"Service '{request.ServiceId}' latency ({Math.Round(avgLatency, 0)}ms) exceeds threshold ({request.LatencyAlertThresholdMs}ms).",
                        AffectedDependents: request.DependentServiceIds.Count,
                        MetricValue: (decimal)avgLatency,
                        Threshold: request.LatencyAlertThresholdMs));
                    rootDegraded = true;
                }

                var avgErrorPercent = (decimal)avgError * 100m;
                if (avgErrorPercent > request.ErrorRateAlertThreshold)
                {
                    alerts.Add(new TopologyAlert(
                        ServiceId: request.ServiceId,
                        AlertType: "HighErrorRate",
                        Severity: "Critical",
                        Message: $"Service '{request.ServiceId}' error rate ({Math.Round(avgErrorPercent, 2)}%) exceeds threshold ({request.ErrorRateAlertThreshold}%). Affects {request.DependentServiceIds.Count} dependent services.",
                        AffectedDependents: request.DependentServiceIds.Count,
                        MetricValue: avgErrorPercent,
                        Threshold: request.ErrorRateAlertThreshold));
                    rootDegraded = true;
                }
            }

            // Alerta de propagação se o serviço raiz está degradado e há dependentes
            if (rootDegraded && request.DependentServiceIds.Count > 0)
            {
                alerts.Add(new TopologyAlert(
                    ServiceId: request.ServiceId,
                    AlertType: "PropagationRisk",
                    Severity: "Warning",
                    Message: $"Degradation in '{request.ServiceId}' may propagate to {request.DependentServiceIds.Count} dependent service(s): {string.Join(", ", request.DependentServiceIds.Take(5))}.",
                    AffectedDependents: request.DependentServiceIds.Count,
                    MetricValue: request.DependentServiceIds.Count,
                    Threshold: 1m));
            }

            return Result<Response>.Success(new Response(
                request.ServiceId,
                request.Environment,
                request.DependentServiceIds.Count,
                alerts,
                EvaluatedAt: now));
        }
    }

    public sealed record TopologyAlert(
        string ServiceId,
        string AlertType,
        string Severity,
        string Message,
        int AffectedDependents,
        decimal MetricValue,
        decimal Threshold);

    public sealed record Response(
        string ServiceId,
        string Environment,
        int DependentServiceCount,
        IReadOnlyList<TopologyAlert> Alerts,
        DateTimeOffset EvaluatedAt);
}
