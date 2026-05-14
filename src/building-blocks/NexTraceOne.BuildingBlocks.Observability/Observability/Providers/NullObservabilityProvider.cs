using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Providers;

/// <summary>
/// Implementação null do IObservabilityProvider para uso quando Elastic/ClickHouse não estão disponíveis.
/// Retorna coleções vazias e indica unhealthy, permitindo que a aplicação funcione sem observabilidade externa.
/// </summary>
internal sealed class NullObservabilityProvider : IObservabilityProvider
{
    public string ProviderName => "None";

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false); // Indica que observabilidade externa não está disponível
    }

    public Task<IReadOnlyList<LogEntry>> QueryLogsAsync(
        LogQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<LogEntry>>(Array.Empty<LogEntry>());
    }

    public Task<IReadOnlyList<TraceSummary>> QueryTracesAsync(
        TraceQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TraceSummary>>(Array.Empty<TraceSummary>());
    }

    public Task<TraceDetail?> GetTraceDetailAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TraceDetail?>(null);
    }

    public Task<IReadOnlyList<TelemetryMetricPoint>> QueryMetricsAsync(
        MetricQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TelemetryMetricPoint>>(Array.Empty<TelemetryMetricPoint>());
    }
}
