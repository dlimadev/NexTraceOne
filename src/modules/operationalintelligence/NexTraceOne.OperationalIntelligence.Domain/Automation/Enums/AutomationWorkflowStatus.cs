namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

/// <summary>
/// Estado do ciclo de vida de um workflow de automação operacional.
/// Permite rastrear a progressão completa desde rascunho até conclusão ou cancelamento.
/// </summary>
public enum AutomationWorkflowStatus
{
    /// <summary>Rascunho — workflow ainda não submetido.</summary>
    Draft = 0,

    /// <summary>Aguardando verificação de pré-condições.</summary>
    PendingPreconditions = 1,

    /// <summary>Aguardando aprovação para execução.</summary>
    AwaitingApproval = 2,

    /// <summary>Aprovado — autorizado para execução.</summary>
    Approved = 3,

    /// <summary>Pronto para execução — todas as condições satisfeitas.</summary>
    ReadyToExecute = 4,

    /// <summary>Em execução — ações de automação em andamento.</summary>
    Executing = 5,

    /// <summary>Aguardando validação pós-execução.</summary>
    AwaitingValidation = 6,

    /// <summary>Concluído — workflow finalizado com sucesso.</summary>
    Completed = 7,

    /// <summary>Falhado — workflow terminou com erro.</summary>
    Failed = 8,

    /// <summary>Cancelado — workflow interrompido antes da conclusão.</summary>
    Cancelled = 9,

    /// <summary>Rejeitado — workflow não aprovado para execução.</summary>
    Rejected = 10
}
