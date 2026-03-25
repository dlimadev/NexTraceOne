using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;

namespace NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

/// <summary>
/// Aggregate Root que representa uma instância em execução de um workflow de aprovação.
/// Controla o ciclo de vida da aprovação: Draft → Pending → InReview → Approved/Rejected/Cancelled.
/// </summary>
public sealed class WorkflowInstance : AggregateRoot<WorkflowInstanceId>
{
    private WorkflowInstance() { }

    /// <summary>Identificador do template de workflow utilizado.</summary>
    public WorkflowTemplateId WorkflowTemplateId { get; private set; } = null!;

    /// <summary>Identificador da release associada a esta instância (módulo ChangeIntelligence).</summary>
    public Guid ReleaseId { get; private set; }

    /// <summary>Identificador do usuário que submeteu o workflow.</summary>
    public string SubmittedBy { get; private set; } = string.Empty;

    /// <summary>Status atual do workflow.</summary>
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;

    /// <summary>Índice do estágio atualmente ativo (zero-based).</summary>
    public int CurrentStageIndex { get; private set; }

    /// <summary>Data/hora UTC em que o workflow foi submetido.</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>Data/hora UTC em que o workflow foi concluído (aprovado, rejeitado ou cancelado).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova instância de workflow vinculada a um template e a uma release.
    /// </summary>
    public static WorkflowInstance Create(
        WorkflowTemplateId workflowTemplateId,
        Guid releaseId,
        string submittedBy,
        DateTimeOffset submittedAt)
    {
        Guard.Against.Null(workflowTemplateId);
        Guard.Against.Default(releaseId);
        Guard.Against.NullOrWhiteSpace(submittedBy);

        return new WorkflowInstance
        {
            Id = WorkflowInstanceId.New(),
            WorkflowTemplateId = workflowTemplateId,
            ReleaseId = releaseId,
            SubmittedBy = submittedBy,
            Status = WorkflowStatus.Draft,
            CurrentStageIndex = 0,
            SubmittedAt = submittedAt
        };
    }

    /// <summary>
    /// Avança o workflow para o próximo estágio, incrementando o índice atual.
    /// Se o status for Draft ou Pending, transiciona automaticamente para InReview.
    /// </summary>
    public Result<Unit> Advance()
    {
        if (IsTerminal(Status))
            return WorkflowErrors.WorkflowAlreadyCompleted(Id.Value.ToString());

        if (Status is WorkflowStatus.Draft or WorkflowStatus.Pending)
            Status = WorkflowStatus.InReview;

        CurrentStageIndex++;
        return Unit.Value;
    }

    /// <summary>
    /// Finaliza o workflow com o status informado (Approved ou Rejected).
    /// Retorna falha se a transição não for válida ou se o workflow já estiver concluído.
    /// </summary>
    public Result<Unit> Complete(WorkflowStatus finalStatus, DateTimeOffset completedAt)
    {
        if (IsTerminal(Status))
            return WorkflowErrors.WorkflowAlreadyCompleted(Id.Value.ToString());

        if (finalStatus is not (WorkflowStatus.Approved or WorkflowStatus.Rejected))
            return WorkflowErrors.InvalidStatusTransition(Status.ToString(), finalStatus.ToString());

        var transitionResult = ValidateTransition(finalStatus);
        if (transitionResult.IsFailure)
            return transitionResult;

        Status = finalStatus;
        CompletedAt = completedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Cancela o workflow, desde que não esteja em estado terminal.
    /// </summary>
    public Result<Unit> Cancel(DateTimeOffset cancelledAt)
    {
        if (IsTerminal(Status))
            return WorkflowErrors.WorkflowAlreadyCompleted(Id.Value.ToString());

        Status = WorkflowStatus.Cancelled;
        CompletedAt = cancelledAt;
        return Unit.Value;
    }

    /// <summary>
    /// Valida se a transição do status atual para o novo status é permitida.
    /// </summary>
    public Result<Unit> ValidateTransition(WorkflowStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
            return WorkflowErrors.InvalidStatusTransition(Status.ToString(), newStatus.ToString());

        return Unit.Value;
    }

    /// <summary>
    /// Verifica se o status informado representa um estado terminal (Approved, Rejected, Cancelled).
    /// </summary>
    private static bool IsTerminal(WorkflowStatus status) =>
        status is WorkflowStatus.Approved or WorkflowStatus.Rejected or WorkflowStatus.Cancelled;

    /// <summary>
    /// Verifica se a transição de status é válida segundo o ciclo de vida do workflow.
    /// Transições permitidas:
    /// Draft → Pending, InReview, Cancelled
    /// Pending → InReview, Cancelled
    /// InReview → Approved, Rejected, Cancelled
    /// </summary>
    private static bool IsValidTransition(WorkflowStatus from, WorkflowStatus to) =>
        (from, to) switch
        {
            (WorkflowStatus.Draft, WorkflowStatus.Pending) => true,
            (WorkflowStatus.Draft, WorkflowStatus.InReview) => true,
            (WorkflowStatus.Draft, WorkflowStatus.Cancelled) => true,
            (WorkflowStatus.Pending, WorkflowStatus.InReview) => true,
            (WorkflowStatus.Pending, WorkflowStatus.Cancelled) => true,
            (WorkflowStatus.InReview, WorkflowStatus.Approved) => true,
            (WorkflowStatus.InReview, WorkflowStatus.Rejected) => true,
            (WorkflowStatus.InReview, WorkflowStatus.Cancelled) => true,
            _ => false
        };
}

/// <summary>Identificador fortemente tipado de WorkflowInstance.</summary>
public sealed record WorkflowInstanceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkflowInstanceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkflowInstanceId From(Guid id) => new(id);
}
