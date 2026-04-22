namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;

/// <summary>
/// Interface de leitura analítica para ClickHouse.
/// Fornece queries de latência de trace, agregações de métricas e contagem de logs
/// para suportar dashboards operacionais contextualizados por serviço, ambiente e período.
/// </summary>
public interface IClickHouseAnalyticsReader
{
    /// <summary>
    /// Retorna o resumo de latência de traces para um serviço e ambiente no período indicado.
    /// </summary>
    Task<IReadOnlyList<TraceLatencySummary>> GetTraceLatencySummaryAsync(
        string serviceName,
        string environment,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>
    /// Retorna agregações de uma métrica específica por serviço no período indicado.
    /// </summary>
    Task<IReadOnlyList<MetricAggregation>> GetMetricAggregationAsync(
        string metricName,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>
    /// Retorna a contagem de logs por serviço, nível e período.
    /// </summary>
    Task<long> GetLogCountAsync(
        string serviceName,
        string level,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}

/// <summary>
/// Resumo de latência de traces para um serviço/ambiente num período.
/// </summary>
public sealed record TraceLatencySummary(
    string ServiceName,
    string Environment,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    long SampleCount,
    DateTimeOffset PeriodStart);

/// <summary>
/// Agregação de uma métrica para um serviço num período.
/// </summary>
public sealed record MetricAggregation(
    string MetricName,
    string ServiceName,
    double Sum,
    double Avg,
    double Max,
    long DataPoints,
    DateTimeOffset PeriodStart);
