using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateServiceMetrics;

/// <summary>
/// Feature: CorrelateServiceMetrics — correlaciona automaticamente métricas de múltiplos
/// serviços para detetar padrões de propagação de degradação entre dependências.
/// Correlação baseada em janela temporal e delta percentual de latência/erros.
/// </summary>
public static class CorrelateServiceMetrics
{
    public sealed record Query(
        IReadOnlyList<string> ServiceIds,
        string Environment,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        /// <summary>Delta mínimo (%) para considerar correlação significativa. Padrão: 10%.</summary>
        decimal CorrelationThresholdPercent = 10m) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceIds).NotNull().NotEmpty().Must(ids => ids.Count <= 20)
                .WithMessage("Maximum 20 services per correlation request.");
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.WindowEnd).GreaterThan(x => x.WindowStart)
                .WithMessage("WindowEnd must be after WindowStart.");
            RuleFor(x => x.CorrelationThresholdPercent).InclusiveBetween(1m, 100m);
        }
    }

    public sealed class Handler(
        IRuntimeSnapshotRepository snapshotRepository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var correlations = new List<ServiceMetricCorrelation>();

            // Recolhe snapshots de cada serviço na janela temporal
            var serviceMetrics = new Dictionary<string, (decimal AvgLatency, decimal AvgErrorRate, int SnapshotCount)>();

            foreach (var serviceId in request.ServiceIds)
            {
                var snapshots = await snapshotRepository.ListByServiceAsync(
                    serviceId, request.Environment, 1, 200, cancellationToken);

                var windowSnapshots = snapshots
                    .Where(s => s.CapturedAt >= request.WindowStart && s.CapturedAt <= request.WindowEnd)
                    .ToList();

                if (windowSnapshots.Count == 0) continue;

                var avgLatency = windowSnapshots.Average(s => (double)s.AvgLatencyMs);
                var avgErrorRate = windowSnapshots.Average(s => (double)s.ErrorRate);
                serviceMetrics[serviceId] = ((decimal)avgLatency, (decimal)avgErrorRate, windowSnapshots.Count);
            }

            // Correlaciona pares de serviços com degradação simultânea
            var serviceList = serviceMetrics.Keys.ToList();
            for (var i = 0; i < serviceList.Count; i++)
            {
                for (var j = i + 1; j < serviceList.Count; j++)
                {
                    var a = serviceList[i];
                    var b = serviceList[j];
                    var (latencyA, errorA, countA) = serviceMetrics[a];
                    var (latencyB, errorB, countB) = serviceMetrics[b];

                    // Correlação de latência: detetar se ambos degradaram na mesma janela
                    if (latencyA == 0 || latencyB == 0) continue;

                    var latencyDelta = Math.Abs((latencyA - latencyB) / Math.Max(latencyA, latencyB) * 100);
                    var errorDelta = Math.Abs(errorA - errorB);

                    // Correlação alta: delta < threshold significa comportamento similar
                    var isCorrelated = latencyDelta < request.CorrelationThresholdPercent;
                    var strength = isCorrelated
                        ? Math.Max(0m, Math.Round(100m - latencyDelta, 1))
                        : 0m;

                    if (isCorrelated)
                    {
                        correlations.Add(new ServiceMetricCorrelation(
                            ServiceIdA: a,
                            ServiceIdB: b,
                            CorrelationStrengthPercent: strength,
                            AvgLatencyDeltaPercent: Math.Round(latencyDelta, 1),
                            AvgErrorRateDelta: Math.Round(errorDelta, 4),
                            DataPointsA: countA,
                            DataPointsB: countB));
                    }
                }
            }

            return Result<Response>.Success(new Response(
                request.ServiceIds,
                request.Environment,
                request.WindowStart,
                request.WindowEnd,
                correlations.OrderByDescending(c => c.CorrelationStrengthPercent).ToList(),
                clock.UtcNow));
        }
    }

    public sealed record ServiceMetricCorrelation(
        string ServiceIdA,
        string ServiceIdB,
        decimal CorrelationStrengthPercent,
        decimal AvgLatencyDeltaPercent,
        decimal AvgErrorRateDelta,
        int DataPointsA,
        int DataPointsB);

    public sealed record Response(
        IReadOnlyList<string> AnalyzedServiceIds,
        string Environment,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        IReadOnlyList<ServiceMetricCorrelation> Correlations,
        DateTimeOffset AnalyzedAt);
}
