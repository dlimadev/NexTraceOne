namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Evento de observabilidade armazenado no ClickHouse.
/// Representa métricas, logs, traces e health checks do runtime.
/// Otimizado para queries analíticas de alta performance em dados de telemetria.
/// </summary>
public sealed record ClickHouseEvent(
    DateTime Timestamp,
    string EventId,
    string EventType, // request, error, metric, log, trace
    string ServiceName,
    string Environment,
    string? TraceId,
    string? SpanId,
    string? UserId,
    string? Endpoint,
    string? HttpMethod,
    int? StatusCode,
    double? DurationMs,
    string? ErrorMessage,
    string? ErrorType,
    Dictionary<string, string> Tags,
    Dictionary<string, object> Metadata);

/// <summary>
/// Métricas de requisições agregadas por time bucket e endpoint.
/// Inclui latência (avg, p50, p95, p99), throughput e error rate.
/// Usado para dashboards de performance e SLO monitoring.
/// </summary>
public sealed record RequestMetrics(
    DateTime TimeBucket,
    string Endpoint,
    string HttpMethod,
    long RequestCount,
    double AvgDurationMs,
    double P50DurationMs,
    double P95DurationMs,
    double P99DurationMs,
    long ErrorCount,
    double ErrorRate);

/// <summary>
/// Analytics de erros agrupados por tipo com contexto de ocorrência.
/// Inclui stack traces amostrais e endpoints afetados para debugging.
/// Essencial para identificação rápida de padrões de falha.
/// </summary>
public sealed record ErrorAnalytics(
    DateTime TimeBucket,
    string ErrorType,
    string ErrorMessage,
    string ServiceName,
    long OccurrenceCount,
    List<string> AffectedEndpoints,
    List<string> SampleStackTraces);

/// <summary>
/// Métricas de atividade de usuários (ações, sessões, navegação).
/// Usado para análise de comportamento e otimização de UX.
/// </summary>
public sealed record UserActivityMetrics(
    DateTime TimeBucket,
    string UserId,
    long ActionCount,
    List<string> TopEndpoints,
    double AvgSessionDurationMinutes);

/// <summary>
/// Métricas de saúde do sistema (CPU, memória, disco, conexões, RPS).
/// Monitoramento em tempo real da infraestrutura e capacity planning.
/// </summary>
public sealed record SystemHealthMetrics(
    DateTime Timestamp,
    string ServiceName,
    double CpuUsagePercent,
    double MemoryUsageMB,
    double DiskUsagePercent,
    int ActiveConnections,
    double RequestsPerSecond,
    double ErrorRatePercent);
