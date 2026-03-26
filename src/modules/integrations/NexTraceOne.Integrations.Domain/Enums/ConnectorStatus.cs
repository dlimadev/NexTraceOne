namespace NexTraceOne.Integrations.Domain.Enums;

/// <summary>
/// Estado operacional de um connector de integração.
/// </summary>
public enum ConnectorStatus
{
    /// <summary>
    /// Connector configurado mas ainda não ativo.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Connector ativo e operacional.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Connector temporariamente pausado.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Connector desativado manualmente.
    /// </summary>
    Disabled = 3,

    /// <summary>
    /// Connector com falhas que impedem operação.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Connector em processo de configuração inicial.
    /// </summary>
    Configuring = 5
}
