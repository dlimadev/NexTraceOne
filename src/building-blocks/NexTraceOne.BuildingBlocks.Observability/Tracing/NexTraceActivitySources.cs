using System.Diagnostics;

namespace NexTraceOne.BuildingBlocks.Observability.Tracing;

/// <summary>
/// Activity Sources centralizados para OpenTelemetry.
/// Cada módulo pode criar spans filhos destes sources principais.
///
/// Registrados automaticamente via AddBuildingBlocksObservability().
/// Spans criados com estes sources são exportados via OTLP para o Collector,
/// que roteia para o Telemetry Store (Tempo) e gera métricas derivadas (SpanMetrics).
/// </summary>
public static class NexTraceActivitySources
{
    /// <summary>Source para operações de command (escrita).</summary>
    public static readonly ActivitySource Commands = new("NexTraceOne.Commands");

    /// <summary>Source para operações de query (leitura).</summary>
    public static readonly ActivitySource Queries = new("NexTraceOne.Queries");

    /// <summary>Source para eventos de domínio e integração.</summary>
    public static readonly ActivitySource Events = new("NexTraceOne.Events");

    /// <summary>Source para chamadas HTTP externas (adapters).</summary>
    public static readonly ActivitySource ExternalHttp = new("NexTraceOne.ExternalHttp");

    /// <summary>
    /// Source para operações do pipeline de telemetria (agregação, retenção, correlação).
    /// Permite rastrear a própria execução dos jobs de consolidação e cleanup.
    /// </summary>
    public static readonly ActivitySource TelemetryPipeline = new("NexTraceOne.TelemetryPipeline");
}
