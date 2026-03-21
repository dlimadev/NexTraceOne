namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado de uma execução de agent.
/// Pending: aguarda início. Running: em execução.
/// Completed: terminada com sucesso. Failed: terminada com erro.
/// Cancelled: cancelada pelo utilizador ou sistema.
/// </summary>
public enum AgentExecutionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
}
