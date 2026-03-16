using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;

/// <summary>
/// Catálogo centralizado de erros do subdomínio Automation com códigos i18n.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: Automation.{Entidade}.{Descrição}
/// </summary>
public static class AutomationErrors
{
    /// <summary>Ação de automação não encontrada pelo identificador informado.</summary>
    public static Error ActionNotFound(string actionId)
        => Error.NotFound(
            "Automation.Action.NotFound",
            "Automation action '{0}' was not found.",
            actionId);

    /// <summary>Workflow de automação não encontrado pelo identificador informado.</summary>
    public static Error WorkflowNotFound(string workflowId)
        => Error.NotFound(
            "Automation.Workflow.NotFound",
            "Automation workflow '{0}' was not found.",
            workflowId);

    /// <summary>Transição de estado inválida no workflow de automação.</summary>
    public static Error InvalidWorkflowTransition(string currentStatus, string targetStatus)
        => Error.Validation(
            "Automation.Workflow.InvalidTransition",
            "Cannot transition from '{0}' to '{1}'.",
            currentStatus,
            targetStatus);

    /// <summary>Pré-condições não satisfeitas para execução do workflow.</summary>
    public static Error PreconditionsNotMet(string workflowId)
        => Error.Validation(
            "Automation.Workflow.PreconditionsNotMet",
            "Preconditions not met for workflow '{0}'.",
            workflowId);

    /// <summary>Aprovação obrigatória antes da execução do workflow.</summary>
    public static Error ApprovalRequired(string workflowId)
        => Error.Validation(
            "Automation.Workflow.ApprovalRequired",
            "Approval required before executing workflow '{0}'.",
            workflowId);

    /// <summary>Persona não autorizada a executar a ação de automação.</summary>
    public static Error UnauthorizedAction(string actionId, string persona)
        => Error.Forbidden(
            "Automation.Action.Unauthorized",
            "Persona '{1}' is not authorized to execute action '{0}'.",
            actionId,
            persona);

    /// <summary>Workflow de automação já concluído — não pode ser alterado.</summary>
    public static Error WorkflowAlreadyCompleted(string workflowId)
        => Error.Conflict(
            "Automation.Workflow.AlreadyCompleted",
            "Workflow '{0}' is already completed.",
            workflowId);

    /// <summary>Ação inválida para o estado atual do workflow.</summary>
    public static Error InvalidAction(string action)
        => Error.Validation(
            "Automation.Workflow.InvalidAction",
            "Action '{0}' is not valid for this workflow state.",
            action);
}
