namespace NexTraceOne.BuildingBlocks.Observability.Ingestion;

/// <summary>
/// Implementação nula de IIngestionMetricsCollector.
/// Usada quando Ingestion:Metrics:Enabled = false — graceful degradation sem overhead.
/// </summary>
internal sealed class NullIngestionMetricsCollector : IIngestionMetricsCollector
{
    public void RecordEventReceived(string tenantId, string source) { }
    public void RecordEventProcessed(string tenantId, string result) { }
    public void RecordProcessingDuration(string tenantId, string stage, double durationMs) { }
    public void RecordDlqEntry(string tenantId) { }
}
