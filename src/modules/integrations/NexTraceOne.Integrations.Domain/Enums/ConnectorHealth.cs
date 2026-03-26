namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Saúde operacional de um connector de integração.
/// </summary>
public enum ConnectorHealth
{
    /// <summary>
    /// Saúde desconhecida (sem dados suficientes).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Connector saudável, sem problemas detectados.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Connector com problemas intermitentes ou parciais.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Connector não saudável, com falhas frequentes.
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// Connector em estado crítico, requer atenção imediata.
    /// </summary>
    Critical = 4
}
