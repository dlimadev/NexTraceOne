namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Tipo de métrica gerada por uma regra LogToMetricRule.
/// </summary>
public enum MetricType
{
    /// <summary>Contador monotonicamente crescente.</summary>
    Counter = 1,

    /// <summary>Valor instantâneo que pode subir ou descer.</summary>
    Gauge = 2,

    /// <summary>Distribuição de valores com percentis.</summary>
    Histogram = 3
}
