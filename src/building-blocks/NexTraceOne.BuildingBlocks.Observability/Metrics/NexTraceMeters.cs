using System.Diagnostics.Metrics;

namespace NexTraceOne.BuildingBlocks.Observability.Metrics;

/// <summary>
/// Meters customizados para métricas de negócio da plataforma.
/// </summary>
public static class NexTraceMeters
{
    private static readonly Meter Meter = new("NexTraceOne", "1.0.0");

    /// <summary>Contador de deploys notificados à plataforma.</summary>
    public static readonly Counter<long> DeploymentsNotified = Meter.CreateCounter<long>("nextraceone.deployments.notified");

    /// <summary>Contador de workflows iniciados.</summary>
    public static readonly Counter<long> WorkflowsInitiated = Meter.CreateCounter<long>("nextraceone.workflows.initiated");

    /// <summary>Histograma de duração de cálculo de blast radius (ms).</summary>
    public static readonly Histogram<double> BlastRadiusDuration = Meter.CreateHistogram<double>("nextraceone.blastradius.duration_ms");
}
