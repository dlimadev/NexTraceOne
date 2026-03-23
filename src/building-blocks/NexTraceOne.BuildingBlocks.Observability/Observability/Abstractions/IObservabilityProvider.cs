using NexTraceOne.BuildingBlocks.Observability.Observability.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;

// IMPLEMENTATION STATUS: Contract defined — implementations in Infrastructure.Providers.
// ClickHouse and Elastic implementations will be registered via DI based on configuration.
// Do NOT reference provider internals from Domain or Application layers.

/// <summary>
/// Abstração principal do provider de observabilidade do NexTraceOne.
/// Permite consulta unificada de logs, traces e métricas crus armazenados
/// em ClickHouse ou Elastic, sem acoplar o domínio ao storage.
///
/// O provider é selecionado por configuração (Telemetry:ObservabilityProvider:Provider).
/// O NexTraceOne consome dados via esta interface para análise, correlação, IA interna
/// e apresentação funcional nas telas do produto.
/// </summary>
public interface IObservabilityProvider
{
    /// <summary>Nome do provider ativo ("ClickHouse" ou "Elastic").</summary>
    string ProviderName { get; }

    /// <summary>Verifica se o provider está acessível e operacional.</summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta logs crus por filtro (serviço, ambiente, nível, intervalo de tempo).
    /// Retorna resultados paginados orientados ao produto.
    /// </summary>
    Task<IReadOnlyList<LogEntry>> QueryLogsAsync(
        LogQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta traces/spans por filtro (serviço, operação, ambiente, duração, status).
    /// Retorna resumo de traces orientado ao produto.
    /// </summary>
    Task<IReadOnlyList<TraceSummary>> QueryTracesAsync(
        TraceQueryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém detalhes completos de um trace por trace_id, incluindo todos os spans.
    /// </summary>
    Task<TraceDetail?> GetTraceDetailAsync(
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta métricas técnicas agregadas por serviço/ambiente.
    /// CPU, memória, request duration, error rate, throughput.
    /// </summary>
    Task<IReadOnlyList<TelemetryMetricPoint>> QueryMetricsAsync(
        MetricQueryFilter filter,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Serviço de consultas de telemetria orientadas ao produto.
/// Não é apenas "ler log bruto" ou "ler span bruto" — é análise orientada
/// ao negócio e à operação do NexTraceOne.
/// </summary>
public interface ITelemetryQueryService
{
    /// <summary>Obtém os erros mais frequentes por ambiente num intervalo de tempo.</summary>
    Task<IReadOnlyList<ErrorFrequency>> GetTopErrorsByEnvironmentAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        int top = 10,
        CancellationToken cancellationToken = default);

    /// <summary>Compara latência entre dois ambientes (ex: homologação vs produção).</summary>
    Task<LatencyComparison> CompareLatencyAsync(
        string serviceName,
        string environmentA,
        string environmentB,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>Detecta aumento de falhas após release num serviço.</summary>
    Task<PostReleaseAnalysis> AnalyzePostReleaseImpactAsync(
        string serviceName,
        string environment,
        DateTimeOffset releaseTimestamp,
        TimeSpan windowBefore,
        TimeSpan windowAfter,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém consumo de recursos (CPU, memória) por serviço.</summary>
    Task<IReadOnlyList<ResourceUsageSummary>> GetResourceUsageByServiceAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>Identifica operações com regressão de performance.</summary>
    Task<IReadOnlyList<OperationRegression>> DetectPerformanceRegressionsAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>Correlaciona log com trace por trace_id.</summary>
    Task<CorrelatedSignals> CorrelateByTraceIdAsync(
        string traceId,
        CancellationToken cancellationToken = default);

    /// <summary>Gera evidências agregadas para score de risco de release.</summary>
    Task<ReleaseRiskEvidence> GenerateReleaseRiskEvidenceAsync(
        string serviceName,
        string environment,
        DateTimeOffset releaseTimestamp,
        CancellationToken cancellationToken = default);

    /// <summary>Fornece dados agregados para a IA interna do NexTraceOne.</summary>
    Task<EnvironmentTelemetrySnapshot> GetEnvironmentSnapshotForAiAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstração para estratégia de coleta por ambiente.
/// Desacopla a aplicação do mecanismo de coleta (Collector vs CLR Profiler).
/// </summary>
public interface ICollectionModeStrategy
{
    /// <summary>Nome do modo de coleta ativo ("OpenTelemetryCollector" ou "ClrProfiler").</summary>
    string ModeName { get; }

    /// <summary>Verifica se o modo de coleta está operacional.</summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtém a configuração de exportação do modo de coleta ativo.</summary>
    CollectionExportConfig GetExportConfig();
}

/// <summary>
/// Configuração de exportação do modo de coleta, agnóstica ao mecanismo.
/// </summary>
public sealed record CollectionExportConfig
{
    /// <summary>Endpoint OTLP para envio de telemetria.</summary>
    public required string OtlpEndpoint { get; init; }

    /// <summary>Protocolo de exportação: "grpc" ou "http".</summary>
    public string Protocol { get; init; } = "grpc";

    /// <summary>Indica se exporta para Collector intermediário ou direto para provider.</summary>
    public bool UsesCollectorProxy { get; init; } = true;
}
