namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado operacional de uma fonte de ingestão.
/// </summary>
public enum SourceStatus
{
    /// <summary>
    /// Fonte pendente de configuração.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Fonte ativa e recebendo dados.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Fonte temporariamente pausada.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Fonte desativada.
    /// </summary>
    Disabled = 3,

    /// <summary>
    /// Fonte com erro que impede operação.
    /// </summary>
    Error = 4
}
