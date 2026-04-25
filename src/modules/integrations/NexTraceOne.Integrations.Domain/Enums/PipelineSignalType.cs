namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Tipo de sinal de telemetria ao qual uma regra de pipeline se aplica.
/// </summary>
public enum PipelineSignalType
{
    /// <summary>Aplica-se a spans de tracing (OTel spans).</summary>
    Span = 1,

    /// <summary>Aplica-se a registos de log.</summary>
    Log = 2,

    /// <summary>Aplica-se a métricas.</summary>
    Metric = 3
}
