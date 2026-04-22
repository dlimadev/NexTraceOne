using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;

namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Readers;

/// <summary>
/// Implementação nula do IClickHouseAnalyticsReader.
/// Retorna listas vazias e contagens a zero enquanto o ClickHouse não estiver configurado.
/// Ativa automaticamente quando Analytics:Provider != "ClickHouse" ou ClickHouse indisponível.
/// </summary>
public sealed class NullClickHouseAnalyticsReader : IClickHouseAnalyticsReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<TraceLatencySummary>> GetTraceLatencySummaryAsync(
        string serviceName,
        string environment,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        IReadOnlyList<TraceLatencySummary> empty = [];
        return Task.FromResult(empty);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MetricAggregation>> GetMetricAggregationAsync(
        string metricName,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        IReadOnlyList<MetricAggregation> empty = [];
        return Task.FromResult(empty);
    }

    /// <inheritdoc />
    public Task<long> GetLogCountAsync(
        string serviceName,
        string level,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct) => Task.FromResult(0L);
}
