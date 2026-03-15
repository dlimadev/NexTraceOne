namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Estado do ciclo de vida de um workflow de mitigação.
/// Permite rastrear a progressão completa desde rascunho até conclusão ou cancelamento.
/// </summary>
public enum MitigationWorkflowStatus
{
    /// <summary>Rascunho — workflow ainda não submetido.</summary>
    Draft = 0,

    /// <summary>Recomendado pela IA ou por análise operacional.</summary>
    Recommended = 1,

    /// <summary>Aguardando aprovação para execução.</summary>
    AwaitingApproval = 2,

    /// <summary>Aprovado — autorizado para execução.</summary>
    Approved = 3,

    /// <summary>Em execução — ações de mitigação em andamento.</summary>
    InProgress = 4,

    /// <summary>Aguardando validação pós-execução.</summary>
    AwaitingValidation = 5,

    /// <summary>Concluído — workflow finalizado com sucesso.</summary>
    Completed = 6,

    /// <summary>Rejeitado — workflow não aprovado para execução.</summary>
    Rejected = 7,

    /// <summary>Cancelado — workflow interrompido antes da conclusão.</summary>
    Cancelled = 8
}
