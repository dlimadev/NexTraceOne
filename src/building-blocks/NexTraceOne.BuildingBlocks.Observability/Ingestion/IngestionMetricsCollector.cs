using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Metrics;

namespace NexTraceOne.BuildingBlocks.Observability.Ingestion;

/// <summary>
/// Implementação real de IIngestionMetricsCollector.
/// Emite métricas OTel via IngestionMeters (counter + histogram push instruments).
/// Respeita Ingestion:Metrics:Enabled e Ingestion:Metrics:SamplingRate.
/// </summary>
public sealed class IngestionMetricsCollector(IOptions<IngestionObservabilityOptions> options) : IIngestionMetricsCollector
{
    private readonly IngestionObservabilityOptions _options = options.Value;

    public void RecordEventReceived(string tenantId, string source)
    {
        if (!_options.Enabled) return;
        IngestionMeters.EventsReceived.Add(1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("source", source));
    }

    public void RecordEventProcessed(string tenantId, string result)
    {
        if (!_options.Enabled) return;
        IngestionMeters.EventsProcessed.Add(1,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("result", result));
    }

    public void RecordProcessingDuration(string tenantId, string stage, double durationMs)
    {
        if (!_options.Enabled) return;
        if (_options.SamplingRate < 1.0 && Random.Shared.NextDouble() > _options.SamplingRate) return;
        IngestionMeters.ProcessingDuration.Record(durationMs,
            new KeyValuePair<string, object?>("tenant.id", tenantId),
            new KeyValuePair<string, object?>("stage", stage));
    }

    public void RecordDlqEntry(string tenantId)
    {
        if (!_options.Enabled) return;
        IngestionMeters.DlqEntriesCreated.Add(1,
            new KeyValuePair<string, object?>("tenant.id", tenantId));
    }
}
