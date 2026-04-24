namespace NexTraceOne.BuildingBlocks.Observability.Ingestion;

/// <summary>
/// Colector de métricas do pipeline de ingestão.
/// Emite contadores e histogramas OTel via IngestionMeters.
/// Registado como singleton — implementação real ou nula conforme Ingestion:Metrics:Enabled.
/// </summary>
public interface IIngestionMetricsCollector
{
    /// <summary>Regista um evento recebido na camada de ingestão.</summary>
    void RecordEventReceived(string tenantId, string source);

    /// <summary>Regista o resultado do processamento de um evento no outbox.</summary>
    /// <param name="result">Valores esperados: "success", "failure", "dlq".</param>
    void RecordEventProcessed(string tenantId, string result);

    /// <summary>Regista a duração de uma fase de processamento.</summary>
    /// <param name="stage">Ex: "outbox-cycle", "endpoint-ingest".</param>
    void RecordProcessingDuration(string tenantId, string stage, double durationMs);

    /// <summary>Regista a criação de uma entrada na Dead Letter Queue.</summary>
    void RecordDlqEntry(string tenantId);
}
