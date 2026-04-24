using System.Diagnostics.Metrics;

namespace NexTraceOne.BuildingBlocks.Observability.Metrics;

/// <summary>
/// Meters para métricas do pipeline de ingestão de eventos.
/// Exportados via OTLP no meter "NexTraceOne" (registado em AddBuildingBlocksObservability).
///
/// Dimensões:
/// - tenant.id — isolamento por tenant
/// - source    — origem do evento (provider CI/CD, módulo de outbox)
/// - result    — sucesso/falha/dlq
/// - stage     — fase do pipeline (outbox-cycle, endpoint-ingest)
/// </summary>
public static class IngestionMeters
{
    private static readonly Meter Meter = new(NexTraceMeters.MeterName, "1.0.0");

    /// <summary>Contador de eventos recebidos na camada de ingestão (por tenant + source).</summary>
    public static readonly Counter<long> EventsReceived =
        Meter.CreateCounter<long>("ingestion.events.received");

    /// <summary>Contador de eventos processados pelo outbox (por tenant + result).</summary>
    public static readonly Counter<long> EventsProcessed =
        Meter.CreateCounter<long>("ingestion.events.processed");

    /// <summary>Histograma de duração do ciclo de processamento do outbox (ms, por tenant + stage).</summary>
    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>("ingestion.processing.duration_ms");

    /// <summary>Contador de entradas criadas na Dead Letter Queue (por tenant).</summary>
    public static readonly Counter<long> DlqEntriesCreated =
        Meter.CreateCounter<long>("ingestion.dlq.count");
}
