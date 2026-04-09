namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

/// <summary>
/// Estado de uma execução de playbook operacional.
/// Transições: InProgress → Completed | Failed | Aborted.
/// </summary>
public enum PlaybookExecutionStatus
{
    /// <summary>Execução em andamento.</summary>
    InProgress = 0,

    /// <summary>Execução concluída com sucesso.</summary>
    Completed = 1,

    /// <summary>Execução falhou durante a execução dos passos.</summary>
    Failed = 2,

    /// <summary>Execução abortada manualmente pelo operador.</summary>
    Aborted = 3
}
