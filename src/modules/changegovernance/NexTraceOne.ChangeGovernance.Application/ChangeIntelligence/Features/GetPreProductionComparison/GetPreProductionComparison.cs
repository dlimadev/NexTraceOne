using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPreProductionComparison;

/// <summary>
/// Feature: GetPreProductionComparison — compara o baseline de métricas capturado num ambiente
/// pré-produção (staging) com o baseline de uma release em produção para o mesmo serviço.
///
/// Fornece uma análise diferencial de:
///   - Taxa de erro (ErrorRate)
///   - Latência média, P95, P99
///   - Requests per minute
///   - Throughput
///
/// Cada métrica é classificada como:
///   Improved  — pré-prod melhorou vs produção baseline
///   Degraded  — pré-prod piorou vs produção baseline
///   Stable    — diferença dentro do threshold (±10%)
///   Unknown   — baseline não disponível para uma das releases
///
/// Valor: permite ao Change Advisory usar dados reais de comportamento em staging
/// para aumentar a confiança antes da promoção para produção.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetPreProductionComparison
{
    private const decimal StableThreshold = 0.10m; // ±10%

    /// <summary>
    /// Query para obter a comparação entre baseline de pré-produção e produção.
    /// </summary>
    public sealed record Query(
        Guid PreProductionReleaseId,
        Guid ProductionReleaseId) : IQuery<Response>;

    /// <summary>Valida os IDs das releases.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PreProductionReleaseId).NotEmpty();
            RuleFor(x => x.ProductionReleaseId).NotEmpty();
            RuleFor(x => x)
                .Must(x => x.PreProductionReleaseId != x.ProductionReleaseId)
                .WithMessage("Pre-production and production release IDs must be different.");
        }
    }

    /// <summary>
    /// Handler que obtém os baselines de ambas as releases e compõe o diferencial.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IReleaseBaselineRepository baselineRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var preProdId = ReleaseId.From(request.PreProductionReleaseId);
            var prodId = ReleaseId.From(request.ProductionReleaseId);

            var preProdRelease = await releaseRepository.GetByIdAsync(preProdId, cancellationToken);
            if (preProdRelease is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.PreProductionReleaseId.ToString());

            var prodRelease = await releaseRepository.GetByIdAsync(prodId, cancellationToken);
            if (prodRelease is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ProductionReleaseId.ToString());

            var preProdBaseline = await baselineRepository.GetByReleaseIdAsync(preProdId, cancellationToken);
            var prodBaseline = await baselineRepository.GetByReleaseIdAsync(prodId, cancellationToken);

            if (preProdBaseline is null && prodBaseline is null)
            {
                return new Response(
                    PreProductionReleaseId: request.PreProductionReleaseId,
                    ProductionReleaseId: request.ProductionReleaseId,
                    PreProductionServiceName: preProdRelease.ServiceName,
                    ProductionServiceName: prodRelease.ServiceName,
                    HasBaselineData: false,
                    OverallSignal: "Insufficient",
                    OverallRationale: "No baseline data available for either release.",
                    ErrorRate: null,
                    AvgLatencyMs: null,
                    P95LatencyMs: null,
                    P99LatencyMs: null,
                    RequestsPerMinute: null,
                    Throughput: null);
            }

            var errorRate = BuildMetricDiff("ErrorRate",
                preProdBaseline?.ErrorRate, prodBaseline?.ErrorRate, lowerIsBetter: true);

            var avgLatency = BuildMetricDiff("AvgLatencyMs",
                preProdBaseline?.AvgLatencyMs, prodBaseline?.AvgLatencyMs, lowerIsBetter: true);

            var p95Latency = BuildMetricDiff("P95LatencyMs",
                preProdBaseline?.P95LatencyMs, prodBaseline?.P95LatencyMs, lowerIsBetter: true);

            var p99Latency = BuildMetricDiff("P99LatencyMs",
                preProdBaseline?.P99LatencyMs, prodBaseline?.P99LatencyMs, lowerIsBetter: true);

            var rpm = BuildMetricDiff("RequestsPerMinute",
                preProdBaseline?.RequestsPerMinute, prodBaseline?.RequestsPerMinute, lowerIsBetter: false);

            var throughput = BuildMetricDiff("Throughput",
                preProdBaseline?.Throughput, prodBaseline?.Throughput, lowerIsBetter: false);

            var metrics = new[] { errorRate, avgLatency, p95Latency, p99Latency };
            var degradedCount = metrics.Count(m => m?.Trend == "Degraded");
            var improvedCount = metrics.Count(m => m?.Trend == "Improved");

            var (signal, rationale) = degradedCount >= 2
                ? ("Concerning", $"{degradedCount} key metrics degraded in pre-production compared to production baseline. Promote with caution.")
                : improvedCount >= 2
                    ? ("Positive", $"{improvedCount} key metrics improved in pre-production. Staging behaves better than production baseline.")
                    : ("Neutral", "Metrics are within acceptable range. No significant regressions detected in pre-production.");

            return new Response(
                PreProductionReleaseId: request.PreProductionReleaseId,
                ProductionReleaseId: request.ProductionReleaseId,
                PreProductionServiceName: preProdRelease.ServiceName,
                ProductionServiceName: prodRelease.ServiceName,
                HasBaselineData: true,
                OverallSignal: signal,
                OverallRationale: rationale,
                ErrorRate: errorRate,
                AvgLatencyMs: avgLatency,
                P95LatencyMs: p95Latency,
                P99LatencyMs: p99Latency,
                RequestsPerMinute: rpm,
                Throughput: throughput);
        }

        private static MetricDiff? BuildMetricDiff(
            string metric,
            decimal? preProdValue,
            decimal? prodValue,
            bool lowerIsBetter)
        {
            if (preProdValue is null || prodValue is null)
                return new MetricDiff(metric, preProdValue, prodValue, null, "Unknown");

            if (prodValue == 0m)
                return new MetricDiff(metric, preProdValue, prodValue, null, "Stable");

            var relativeChange = (preProdValue.Value - prodValue.Value) / Math.Abs(prodValue.Value);

            string trend;
            if (Math.Abs(relativeChange) <= StableThreshold)
                trend = "Stable";
            else if (lowerIsBetter)
                trend = relativeChange > 0 ? "Degraded" : "Improved";
            else
                trend = relativeChange > 0 ? "Improved" : "Degraded";

            return new MetricDiff(metric, preProdValue, prodValue, relativeChange * 100m, trend);
        }
    }

    /// <summary>Diferencial de uma métrica entre pré-produção e produção.</summary>
    public sealed record MetricDiff(
        string Metric,
        decimal? PreProductionValue,
        decimal? ProductionValue,
        decimal? RelativeChangePercent,
        string Trend);

    /// <summary>Resposta da comparação pré-produção vs produção.</summary>
    public sealed record Response(
        Guid PreProductionReleaseId,
        Guid ProductionReleaseId,
        string PreProductionServiceName,
        string ProductionServiceName,
        bool HasBaselineData,
        string OverallSignal,
        string OverallRationale,
        MetricDiff? ErrorRate,
        MetricDiff? AvgLatencyMs,
        MetricDiff? P95LatencyMs,
        MetricDiff? P99LatencyMs,
        MetricDiff? RequestsPerMinute,
        MetricDiff? Throughput);
}
