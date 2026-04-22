namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Estado de um plano de execução de agent com suporte a aprovação humana.
/// Pending: aguarda início. Running: em execução.
/// WaitingApproval: aguarda aprovação humana num passo intermédio.
/// Completed: concluído com sucesso. Failed: terminado com erro.
/// Cancelled: cancelado pelo utilizador ou sistema.
/// </summary>
public enum PlanStatus
{
    Pending = 0,
    Running = 1,
    WaitingApproval = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
}
