using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Workflow.Domain.Enums;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Entidade que representa a decisão de um aprovador em um estágio de workflow.
/// Registra quem decidiu, qual ação tomou e o comentário associado.
/// </summary>
public sealed class ApprovalDecision : AuditableEntity<ApprovalDecisionId>
{
    private ApprovalDecision() { }

    /// <summary>Identificador do estágio no qual a decisão foi tomada.</summary>
    public WorkflowStageId WorkflowStageId { get; private set; } = null!;

    /// <summary>Identificador da instância de workflow à qual o estágio pertence.</summary>
    public WorkflowInstanceId WorkflowInstanceId { get; private set; } = null!;

    /// <summary>Identificador do usuário que tomou a decisão.</summary>
    public string DecidedBy { get; private set; } = string.Empty;

    /// <summary>Ação tomada pelo aprovador (Approved, Rejected, RequestedChanges, Observation).</summary>
    public ApprovalAction Decision { get; private set; }

    /// <summary>Comentário do aprovador (obrigatório para Rejected e RequestedChanges).</summary>
    public string? Comment { get; private set; }

    /// <summary>Data/hora UTC em que a decisão foi registrada.</summary>
    public DateTimeOffset DecidedAt { get; private set; }

    /// <summary>
    /// Cria uma nova decisão de aprovação com validações de negócio.
    /// Comentário é obrigatório para ações de Rejected e RequestedChanges.
    /// </summary>
    public static Result<ApprovalDecision> Create(
        WorkflowStageId workflowStageId,
        WorkflowInstanceId workflowInstanceId,
        string decidedBy,
        ApprovalAction decision,
        string? comment,
        DateTimeOffset decidedAt)
    {
        Guard.Against.Null(workflowStageId);
        Guard.Against.Null(workflowInstanceId);
        Guard.Against.NullOrWhiteSpace(decidedBy);

        if (decision == ApprovalAction.Rejected && string.IsNullOrWhiteSpace(comment))
            return WorkflowErrors.CommentRequiredForRejection();

        if (decision == ApprovalAction.RequestedChanges && string.IsNullOrWhiteSpace(comment))
            return WorkflowErrors.CommentRequiredForRequestChanges();

        return new ApprovalDecision
        {
            Id = ApprovalDecisionId.New(),
            WorkflowStageId = workflowStageId,
            WorkflowInstanceId = workflowInstanceId,
            DecidedBy = decidedBy,
            Decision = decision,
            Comment = comment,
            DecidedAt = decidedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ApprovalDecision.</summary>
public sealed record ApprovalDecisionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ApprovalDecisionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ApprovalDecisionId From(Guid id) => new(id);
}
