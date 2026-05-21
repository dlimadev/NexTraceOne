namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Port de leitura de dados de observabilidade para widgets de dashboard.
/// A implementação concreta roteia para Elasticsearch ou ClickHouse
/// conforme a configuração do provider ativo — opaco para o consumidor.
/// </summary>
public interface IDashboardObservabilityReader
{
    /// <summary>Pesquisa logs estruturados com filtros opcionais.</summary>
    Task<DashboardLogsResult> QueryLogsAsync(
        DashboardLogsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Consulta série temporal de uma métrica de sistema.</summary>
    Task<DashboardMetricsResult> QueryMetricsAsync(
        DashboardMetricsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lista traces distribuídos com filtros opcionais.</summary>
    Task<DashboardTracesResult> QueryTracesAsync(
        DashboardTracesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém os erros mais frequentes no período.</summary>
    Task<DashboardErrorsResult> QueryTopErrorsAsync(
        DashboardErrorsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Obtém o mapa de saúde dos serviços activos no período.</summary>
    Task<DashboardServiceHealthResult> QueryServiceHealthAsync(
        DashboardServiceHealthRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna o nome do backend de observabilidade activo ("Elasticsearch" ou "ClickHouse").</summary>
    string BackendName { get; }
}

// ── Request / Response types ────────────────────────────────────────────────

/// <summary>Parâmetros de consulta de logs para um widget de dashboard.</summary>
public sealed record DashboardLogsRequest(
    string? ServiceName,
    string? Environment,
    string? Severity,
    string? SearchText,
    DateTimeOffset From,
    DateTimeOffset Until,
    int Limit = 100);

/// <summary>Entrada de log normalizada para consumo pelo dashboard.</summary>
public sealed record DashboardLogEntry(
    DateTimeOffset Timestamp,
    string Severity,
    string ServiceName,
    string Message,
    string? TraceId,
    string? Environment);

/// <summary>Resultado de consulta de logs, com indicador de disponibilidade do backend.</summary>
public sealed record DashboardLogsResult(
    IReadOnlyList<DashboardLogEntry> Entries,
    long TotalCount,
    bool IsBackendAvailable);

/// <summary>Parâmetros de consulta de métricas para um widget de série temporal.</summary>
public sealed record DashboardMetricsRequest(
    string? ServiceName,
    string? Environment,
    string MetricName,
    DateTimeOffset From,
    DateTimeOffset Until);

/// <summary>Ponto de métrica normalizado para consumo pelo dashboard.</summary>
public sealed record DashboardMetricPoint(
    DateTimeOffset Timestamp,
    double Value,
    string MetricName,
    string? ServiceName);

/// <summary>Resultado de consulta de métricas, com indicador de disponibilidade do backend.</summary>
public sealed record DashboardMetricsResult(
    IReadOnlyList<DashboardMetricPoint> Points,
    string MetricName,
    bool IsBackendAvailable);

/// <summary>Parâmetros de consulta de traces para um widget de dashboard.</summary>
public sealed record DashboardTracesRequest(
    string? ServiceName,
    string? Environment,
    double? MinDurationMs,
    bool? HasErrors,
    DateTimeOffset From,
    DateTimeOffset Until,
    int Limit = 50);

/// <summary>Entrada de trace normalizada para consumo pelo dashboard.</summary>
public sealed record DashboardTraceEntry(
    string TraceId,
    string ServiceName,
    string OperationName,
    double DurationMs,
    bool HasErrors,
    DateTimeOffset StartTime,
    int SpanCount);

/// <summary>Resultado de consulta de traces, com indicador de disponibilidade do backend.</summary>
public sealed record DashboardTracesResult(
    IReadOnlyList<DashboardTraceEntry> Traces,
    bool IsBackendAvailable);

/// <summary>Parâmetros de consulta dos erros mais frequentes.</summary>
public sealed record DashboardErrorsRequest(
    string? Environment,
    DateTimeOffset From,
    DateTimeOffset Until,
    int Top = 10);

/// <summary>Entrada de erro normalizada para consumo pelo dashboard.</summary>
public sealed record DashboardErrorEntry(
    string Message,
    string ServiceName,
    int Count,
    string Severity,
    DateTimeOffset LastSeen);

/// <summary>Resultado de consulta de erros mais frequentes, com indicador de disponibilidade do backend.</summary>
public sealed record DashboardErrorsResult(
    IReadOnlyList<DashboardErrorEntry> Errors,
    int TotalErrorCount,
    bool IsBackendAvailable);

/// <summary>Parâmetros de consulta do mapa de saúde dos serviços.</summary>
public sealed record DashboardServiceHealthRequest(
    string? Environment,
    DateTimeOffset From,
    DateTimeOffset Until);

/// <summary>Entrada de saúde de serviço normalizada para consumo pelo dashboard.</summary>
public sealed record DashboardServiceHealthEntry(
    string ServiceName,
    string HealthStatus,   // "healthy" | "degraded" | "critical"
    double ErrorRate,
    double AvgLatencyMs,
    int TraceCount);

/// <summary>Resultado do mapa de saúde dos serviços, com indicador de disponibilidade do backend.</summary>
public sealed record DashboardServiceHealthResult(
    IReadOnlyList<DashboardServiceHealthEntry> Services,
    bool IsBackendAvailable);
