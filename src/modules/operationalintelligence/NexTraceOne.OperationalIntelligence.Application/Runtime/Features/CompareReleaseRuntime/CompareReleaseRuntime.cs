using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CompareReleaseRuntime;

/// <summary>
/// Feature: CompareReleaseRuntime — compara métricas de runtime entre dois períodos de release.
/// Calcula médias de cada período (before/after) e retorna deltas absolutos e percentuais
/// para latência, taxa de erro, throughput e uso de recursos, permitindo avaliar o impacto de deploys.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CompareReleaseRuntime
{
    /// <summary>Query para comparar métricas de runtime entre dois períodos de release.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        DateTimeOffset BeforePeriodStart,
        DateTimeOffset BeforePeriodEnd,
        DateTimeOffset AfterPeriodStart,
        DateTimeOffset AfterPeriodEnd) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de comparação de runtime entre releases.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.BeforePeriodStart).NotEmpty();
            RuleFor(x => x.BeforePeriodEnd).NotEmpty()
                .GreaterThan(x => x.BeforePeriodStart)
                .WithMessage("Before period end must be after before period start.");
            RuleFor(x => x.AfterPeriodStart).NotEmpty();
            RuleFor(x => x.AfterPeriodEnd).NotEmpty()
                .GreaterThan(x => x.AfterPeriodStart)
                .WithMessage("After period end must be after after period start.");
        }
    }

    /// <summary>
    /// Handler que busca snapshots de ambos os períodos, calcula médias e retorna a comparação.
    /// Métricas comparadas: latência média, P99, taxa de erro, throughput, CPU e memória.
    /// </summary>
    public sealed class Handler(
        IRuntimeSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allSnapshots = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                1,
                1000,
                cancellationToken);

            var beforeSnapshots = allSnapshots
                .Where(s => s.CapturedAt >= request.BeforePeriodStart && s.CapturedAt <= request.BeforePeriodEnd)
                .ToList();

            var afterSnapshots = allSnapshots
                .Where(s => s.CapturedAt >= request.AfterPeriodStart && s.CapturedAt <= request.AfterPeriodEnd)
                .ToList();

            var beforeMetrics = ComputeAverages(beforeSnapshots);
            var afterMetrics = ComputeAverages(afterSnapshots);

            return new Response(
                request.ServiceName,
                request.Environment,
                beforeMetrics,
                afterMetrics,
                beforeSnapshots.Count,
                afterSnapshots.Count,
                ComputeDelta(beforeMetrics.AvgLatencyMs, afterMetrics.AvgLatencyMs),
                ComputeDelta(beforeMetrics.ErrorRate, afterMetrics.ErrorRate),
                ComputeDelta(beforeMetrics.RequestsPerSecond, afterMetrics.RequestsPerSecond));
        }

        /// <summary>Calcula médias das métricas de um conjunto de snapshots.</summary>
        private static MetricsSummary ComputeAverages(IReadOnlyList<RuntimeSnapshot> snapshots)
        {
            if (snapshots.Count == 0)
                return new MetricsSummary(0, 0, 0, 0, 0, 0);

            return new MetricsSummary(
                Math.Round(snapshots.Average(s => s.AvgLatencyMs), 2),
                Math.Round(snapshots.Average(s => s.P99LatencyMs), 2),
                Math.Round(snapshots.Average(s => s.ErrorRate), 4),
                Math.Round(snapshots.Average(s => s.RequestsPerSecond), 2),
                Math.Round(snapshots.Average(s => s.CpuUsagePercent), 2),
                Math.Round(snapshots.Average(s => s.MemoryUsageMb), 2));
        }

        /// <summary>Calcula delta percentual entre dois valores, evitando divisão por zero.</summary>
        private static decimal ComputeDelta(decimal before, decimal after)
        {
            if (before == 0m)
                return after != 0m ? 100m : 0m;

            return Math.Round((after - before) / before * 100m, 2);
        }
    }

    /// <summary>Resposta com comparação de métricas de runtime entre dois períodos de release.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        MetricsSummary BeforeMetrics,
        MetricsSummary AfterMetrics,
        int BeforeDataPoints,
        int AfterDataPoints,
        decimal LatencyDeltaPercent,
        decimal ErrorRateDeltaPercent,
        decimal ThroughputDeltaPercent);

    /// <summary>Resumo estatístico das métricas de runtime de um período.</summary>
    public sealed record MetricsSummary(
        decimal AvgLatencyMs,
        decimal P99LatencyMs,
        decimal ErrorRate,
        decimal RequestsPerSecond,
        decimal CpuUsagePercent,
        decimal MemoryUsageMb);
}
