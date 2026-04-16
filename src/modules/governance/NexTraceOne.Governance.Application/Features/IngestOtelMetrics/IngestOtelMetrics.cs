using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.IngestOtelMetrics;

/// <summary>
/// Feature: IngestOtelMetrics — ingestão de métricas enviadas pelo OpenTelemetry Collector.
/// Recebe batches de métricas OTLP/HTTP do collector, persiste via IOtelMetricRepository
/// e correlaciona com serviços e mudanças quando possível.
///
/// PIPELINE:
///   OtelCollector → POST /api/v1/telemetry/metrics (OTLP JSON) → IngestOtelMetrics.Handler
///     → IOtelMetricRepository.BatchInsertAsync
///     → (opcional) correlação com serviço e mudança activa
///
/// CONTEXTO DO PRODUTO:
/// Observabilidade no NexTraceOne é insumo contextual, não fim isolado.
/// As métricas ingeridas aqui ficam disponíveis para:
/// - Change Intelligence (correlação deploy ↔ degradação de métrica)
/// - AIOps (detecção de anomalia pós-deploy)
/// - Service Reliability (SLO, error budget)
/// - FinOps (desperdício e custo por versão)
/// </summary>
public static class IngestOtelMetrics
{
    /// <summary>Command para ingestão de batch de métricas OTLP.</summary>
    public sealed record Command(
        IReadOnlyList<OtelMetricDataPoint> DataPoints,
        string? SourceCollectorId = null) : ICommand<Response>;

    /// <summary>Validador — garante que cada datapoint tem campos mínimos.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DataPoints)
                .NotEmpty().WithMessage("At least one data point is required.")
                .Must(dp => dp.Count <= 10_000)
                .WithMessage("Batch size exceeds maximum of 10,000 data points.");

            RuleForEach(x => x.DataPoints).ChildRules(dp =>
            {
                dp.RuleFor(x => x.MetricName)
                    .NotEmpty().WithMessage("Metric name is required.");
                dp.RuleFor(x => x.Value)
                    .NotEmpty().WithMessage("Metric value is required.");
                dp.RuleFor(x => x.Timestamp)
                    .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(5))
                    .WithMessage("Metric timestamp cannot be in the future.");
            });
        }
    }

    /// <summary>Handler que persiste os datapoints via IOtelMetricRepository.</summary>
    public sealed class Handler(
        IOtelMetricRepository repository,
        Microsoft.Extensions.Logging.ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Ingesting {Count} OTEL metric data points from collector {CollectorId}",
                request.DataPoints.Count,
                request.SourceCollectorId ?? "unknown");

            var ingested = await repository.BatchInsertAsync(
                request.DataPoints,
                cancellationToken);

            logger.LogInformation(
                "Ingested {Ingested}/{Total} OTEL metric data points",
                ingested, request.DataPoints.Count);

            return Result<Response>.Success(new Response(
                IngestedCount: ingested,
                RejectedCount: request.DataPoints.Count - ingested,
                IngestedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Response da ingestão de métricas.</summary>
    public sealed record Response(
        int IngestedCount,
        int RejectedCount,
        DateTimeOffset IngestedAt);
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Ponto de dado de uma métrica OTLP.
/// Mapeado a partir do formato OTLP/JSON (ou simplificado para ingestão interna).
/// </summary>
public sealed record OtelMetricDataPoint
{
    /// <summary>Nome da métrica (ex: "http.server.request.duration").</summary>
    public string MetricName { get; init; } = string.Empty;

    /// <summary>Valor da métrica (gauge, counter, histograma agrupado).</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>Tipo da métrica (Gauge, Sum, Histogram, ExponentialHistogram).</summary>
    public OtelMetricType MetricType { get; init; } = OtelMetricType.Gauge;

    /// <summary>Timestamp UTC do datapoint.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Atributos de recurso (ex: service.name, service.version, deployment.environment).</summary>
    public IReadOnlyDictionary<string, string> ResourceAttributes { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Atributos da métrica (ex: http.method, http.route, http.status_code).</summary>
    public IReadOnlyDictionary<string, string> MetricAttributes { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Identificador do serviço associado (inferido de service.name, se disponível).</summary>
    public string? ServiceName =>
        ResourceAttributes.TryGetValue("service.name", out var v) ? v : null;

    /// <summary>Versão do serviço associado (inferida de service.version).</summary>
    public string? ServiceVersion =>
        ResourceAttributes.TryGetValue("service.version", out var v) ? v : null;

    /// <summary>Ambiente de execução (inferido de deployment.environment).</summary>
    public string? Environment =>
        ResourceAttributes.TryGetValue("deployment.environment", out var v) ? v : null;
}

/// <summary>Tipo de métrica OTLP.</summary>
public enum OtelMetricType
{
    Gauge,
    Sum,
    Histogram,
    ExponentialHistogram,
    Summary
}
