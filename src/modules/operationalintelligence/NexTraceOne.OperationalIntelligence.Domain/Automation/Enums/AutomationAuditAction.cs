namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;

/// <summary>
/// Ações registadas na trilha de auditoria de automação operacional.
/// Permite rastreabilidade completa de cada evento relevante no ciclo de vida do workflow.
/// </summary>
public enum AutomationAuditAction
{
    /// <summary>Workflow de automação criado.</summary>
    WorkflowCreated = 0,

    /// <summary>Pré-condições avaliadas para o workflow.</summary>
    PreconditionsEvaluated = 1,

    /// <summary>Aprovação solicitada para execução do workflow.</summary>
    ApprovalRequested = 2,

    /// <summary>Aprovação concedida para execução do workflow.</summary>
    ApprovalGranted = 3,

    /// <summary>Aprovação rejeitada — workflow não autorizado.</summary>
    ApprovalRejected = 4,

    /// <summary>Execução do workflow iniciada.</summary>
    ExecutionStarted = 5,

    /// <summary>Passo individual do workflow concluído.</summary>
    StepCompleted = 6,

    /// <summary>Execução do workflow concluída com sucesso.</summary>
    ExecutionCompleted = 7,

    /// <summary>Execução do workflow falhada.</summary>
    ExecutionFailed = 8,

    /// <summary>Validação pós-execução registada.</summary>
    ValidationRecorded = 9,

    /// <summary>Workflow cancelado antes da conclusão.</summary>
    WorkflowCancelled = 10,

    /// <summary>Escalação acionada para nível superior.</summary>
    EscalationTriggered = 11
}
