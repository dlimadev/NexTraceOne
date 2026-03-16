namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

/// <summary>
/// Estado do processo de aprovação de um workflow de automação.
/// Permite rastrear o ciclo de aprovação para auditoria e governança.
/// </summary>
public enum AutomationApprovalStatus
{
    /// <summary>Aprovação não necessária para este workflow.</summary>
    NotRequired = 0,

    /// <summary>Aguardando decisão de aprovação.</summary>
    Pending = 1,

    /// <summary>Aprovado — workflow autorizado para execução.</summary>
    Approved = 2,

    /// <summary>Rejeitado — workflow não autorizado para execução.</summary>
    Rejected = 3,

    /// <summary>Escalado — decisão transferida para nível superior.</summary>
    Escalated = 4
}
