using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Workflow.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Workflow.
/// Cada erro possui código único para rastreabilidade em logs e documentação.
/// Padrão: {Módulo}.{Entidade}.{Descrição}
/// </summary>
public static class WorkflowErrors
{
    /// <summary>Template de workflow não encontrado.</summary>
    public static Error TemplateNotFound(string templateId)
        => Error.NotFound(
            "Workflow.Template.NotFound",
            "Workflow template '{0}' was not found.",
            templateId);

    /// <summary>Instância de workflow não encontrada.</summary>
    public static Error InstanceNotFound(string instanceId)
        => Error.NotFound(
            "Workflow.Instance.NotFound",
            "Workflow instance '{0}' was not found.",
            instanceId);

    /// <summary>Estágio de workflow não encontrado.</summary>
    public static Error StageNotFound(string stageId)
        => Error.NotFound(
            "Workflow.Stage.NotFound",
            "Workflow stage '{0}' was not found.",
            stageId);

    /// <summary>Evidence pack não encontrado.</summary>
    public static Error EvidencePackNotFound(string evidencePackId)
        => Error.NotFound(
            "Workflow.EvidencePack.NotFound",
            "Evidence pack '{0}' was not found.",
            evidencePackId);

    /// <summary>Política de SLA não encontrada.</summary>
    public static Error SlaPolicyNotFound(string slaPolicyId)
        => Error.NotFound(
            "Workflow.SlaPolicy.NotFound",
            "SLA policy '{0}' was not found.",
            slaPolicyId);

    /// <summary>Transição de status de workflow inválida.</summary>
    public static Error InvalidStatusTransition(string from, string to)
        => Error.Conflict(
            "Workflow.Instance.InvalidStatusTransition",
            "Cannot transition workflow status from '{0}' to '{1}'.",
            from,
            to);

    /// <summary>Estágio já foi aprovado.</summary>
    public static Error StageAlreadyApproved(string stageId)
        => Error.Conflict(
            "Workflow.Stage.AlreadyApproved",
            "Stage '{0}' has already been approved.",
            stageId);

    /// <summary>Estágio já foi rejeitado.</summary>
    public static Error StageAlreadyRejected(string stageId)
        => Error.Conflict(
            "Workflow.Stage.AlreadyRejected",
            "Stage '{0}' has already been rejected.",
            stageId);

    /// <summary>Comentário obrigatório ao rejeitar.</summary>
    public static Error CommentRequiredForRejection()
        => Error.Validation(
            "Workflow.ApprovalDecision.CommentRequiredForRejection",
            "A comment is required when rejecting a workflow stage.");

    /// <summary>Comentário obrigatório ao solicitar alterações.</summary>
    public static Error CommentRequiredForRequestChanges()
        => Error.Validation(
            "Workflow.ApprovalDecision.CommentRequiredForRequestChanges",
            "A comment is required when requesting changes.");

    /// <summary>Template já possui o nome informado.</summary>
    public static Error TemplateAlreadyHasName(string name)
        => Error.Conflict(
            "Workflow.Template.AlreadyHasName",
            "Template already has the name '{0}'.",
            name);

    /// <summary>Workflow já foi concluído e não pode ser alterado.</summary>
    public static Error WorkflowAlreadyCompleted(string instanceId)
        => Error.Conflict(
            "Workflow.Instance.AlreadyCompleted",
            "Workflow instance '{0}' has already been completed and cannot be modified.",
            instanceId);

    /// <summary>O submitter não pode aprovar a própria submissão.</summary>
    public static Error CannotApproveOwnSubmission()
        => Error.Business(
            "Workflow.ApprovalDecision.CannotApproveOwnSubmission",
            "A submitter cannot approve their own submission.");
}
