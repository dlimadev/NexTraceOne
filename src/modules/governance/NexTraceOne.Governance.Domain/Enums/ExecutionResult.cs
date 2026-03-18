namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Resultado de uma execução de ingestão.
/// </summary>
public enum ExecutionResult
{
    /// <summary>
    /// Execução ainda em andamento.
    /// </summary>
    Running = 0,

    /// <summary>
    /// Execução concluída com sucesso total.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Execução concluída com sucesso parcial.
    /// </summary>
    PartialSuccess = 2,

    /// <summary>
    /// Execução falhou.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Execução cancelada.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Execução expirou por timeout.
    /// </summary>
    TimedOut = 5
}
