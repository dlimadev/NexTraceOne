using System.Diagnostics.Metrics;

namespace NexTraceOne.BuildingBlocks.Observability.Metrics;

/// <summary>
/// Meters customizados para métricas de negócio e operacionais da plataforma.
/// Exportados via OTLP para o OpenTelemetry Collector.
///
/// Categorias:
/// - Negócio: deploys, workflows, blast radius, releases
/// - Telemetria: ingestão, agregação, retenção, correlação
/// - Pipeline: volume processado, erros de pipeline, latência de consolidação
///
/// O nome do meter ("NexTraceOne") é registrado automaticamente no AddBuildingBlocksObservability().
/// </summary>
public static class NexTraceMeters
{
    /// <summary>Nome do meter principal da plataforma.</summary>
    public const string MeterName = "NexTraceOne";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    // ── Métricas de Negócio ──────────────────────────────────────────

    /// <summary>Contador de deploys notificados à plataforma.</summary>
    public static readonly Counter<long> DeploymentsNotified =
        Meter.CreateCounter<long>("nextraceone.deployments.notified");

    /// <summary>Contador de workflows iniciados.</summary>
    public static readonly Counter<long> WorkflowsInitiated =
        Meter.CreateCounter<long>("nextraceone.workflows.initiated");

    /// <summary>Histograma de duração de cálculo de blast radius (ms).</summary>
    public static readonly Histogram<double> BlastRadiusDuration =
        Meter.CreateHistogram<double>("nextraceone.blastradius.duration_ms");

    // ── Métricas de Pipeline de Telemetria ───────────────────────────

    /// <summary>Contador de snapshots de métricas de serviço persistidos no Product Store.</summary>
    public static readonly Counter<long> ServiceMetricsWritten =
        Meter.CreateCounter<long>("nextraceone.telemetry.service_metrics.written");

    /// <summary>Contador de snapshots de dependência persistidos no Product Store.</summary>
    public static readonly Counter<long> DependencyMetricsWritten =
        Meter.CreateCounter<long>("nextraceone.telemetry.dependency_metrics.written");

    /// <summary>Contador de referências de telemetria criadas (ponteiros para Telemetry Store).</summary>
    public static readonly Counter<long> TelemetryReferencesCreated =
        Meter.CreateCounter<long>("nextraceone.telemetry.references.created");

    /// <summary>Contador de anomalias detectadas.</summary>
    public static readonly Counter<long> AnomaliesDetected =
        Meter.CreateCounter<long>("nextraceone.telemetry.anomalies.detected");

    /// <summary>Contador de correlações release/runtime criadas.</summary>
    public static readonly Counter<long> ReleaseCorrelationsCreated =
        Meter.CreateCounter<long>("nextraceone.telemetry.release_correlations.created");

    /// <summary>Histograma de duração de consolidação de métricas por minuto → hora (ms).</summary>
    public static readonly Histogram<double> AggregationDuration =
        Meter.CreateHistogram<double>("nextraceone.telemetry.aggregation.duration_ms");

    /// <summary>Contador de registros removidos pelo job de retenção.</summary>
    public static readonly Counter<long> RetentionPurgedRecords =
        Meter.CreateCounter<long>("nextraceone.telemetry.retention.purged_records");

    /// <summary>Contador de entradas de topologia observada atualizadas.</summary>
    public static readonly Counter<long> TopologyEntriesUpdated =
        Meter.CreateCounter<long>("nextraceone.telemetry.topology.entries_updated");

    /// <summary>Contador de contextos investigativos criados.</summary>
    public static readonly Counter<long> InvestigationContextsCreated =
        Meter.CreateCounter<long>("nextraceone.telemetry.investigation_contexts.created");
}
