using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Workflow.Domain.Enums;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Entidade que representa um estágio individual dentro de uma instância de workflow.
/// Cada estágio possui seu próprio ciclo de aprovação, SLA e contagem de aprovadores.
/// </summary>
public sealed class WorkflowStage : AuditableEntity<WorkflowStageId>
{
    private WorkflowStage() { }

    /// <summary>Identificador da instância de workflow à qual este estágio pertence.</summary>
    public WorkflowInstanceId WorkflowInstanceId { get; private set; } = null!;

    /// <summary>Nome do estágio (ex.: "Code Review", "Security Review", "Architecture Board").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Ordem sequencial deste estágio dentro do workflow (zero-based).</summary>
    public int StageOrder { get; private set; }

    /// <summary>Status atual do estágio.</summary>
    public StageStatus Status { get; private set; } = StageStatus.Pending;

    /// <summary>Número de aprovações necessárias para aprovar este estágio.</summary>
    public int RequiredApprovers { get; private set; }

    /// <summary>Número de aprovações já registradas neste estágio.</summary>
    public int CurrentApprovals { get; private set; }

    /// <summary>Indica se um comentário é obrigatório ao aprovar ou rejeitar.</summary>
    public bool CommentRequired { get; private set; }

    /// <summary>Duração máxima do SLA em horas (nulo se não houver SLA).</summary>
    public int? SlaDurationHours { get; private set; }

    /// <summary>Data/hora UTC em que o estágio foi iniciado.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Data/hora UTC em que o estágio foi concluído.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Indica se o estágio já foi concluído (aprovado, rejeitado ou ignorado).</summary>
    public bool IsComplete =>
        Status is StageStatus.Approved or StageStatus.Rejected or StageStatus.Skipped;

    /// <summary>
    /// Cria um novo estágio vinculado a uma instância de workflow.
    /// </summary>
    public static WorkflowStage Create(
        WorkflowInstanceId workflowInstanceId,
        string name,
        int stageOrder,
        int requiredApprovers,
        bool commentRequired,
        int? slaDurationHours)
    {
        Guard.Against.Null(workflowInstanceId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Negative(stageOrder);
        Guard.Against.NegativeOrZero(requiredApprovers);

        return new WorkflowStage
        {
            Id = WorkflowStageId.New(),
            WorkflowInstanceId = workflowInstanceId,
            Name = name,
            StageOrder = stageOrder,
            Status = StageStatus.Pending,
            RequiredApprovers = requiredApprovers,
            CurrentApprovals = 0,
            CommentRequired = commentRequired,
            SlaDurationHours = slaDurationHours
        };
    }

    /// <summary>
    /// Inicia o estágio, marcando-o como em revisão com a data/hora informada.
    /// </summary>
    public void Start(DateTimeOffset at)
    {
        Status = StageStatus.InReview;
        StartedAt = at;
    }

    /// <summary>
    /// Registra uma aprovação no estágio. Quando o número de aprovações atinge o mínimo
    /// necessário, o estágio é automaticamente marcado como aprovado.
    /// Retorna falha se o estágio já estiver aprovado ou rejeitado.
    /// </summary>
    public Result<Unit> RecordApproval(DateTimeOffset completedAt)
    {
        if (Status == StageStatus.Approved)
            return WorkflowErrors.StageAlreadyApproved(Id.Value.ToString());

        if (Status == StageStatus.Rejected)
            return WorkflowErrors.StageAlreadyRejected(Id.Value.ToString());

        CurrentApprovals++;

        if (CurrentApprovals >= RequiredApprovers)
        {
            Status = StageStatus.Approved;
            CompletedAt = completedAt;
        }

        return Unit.Value;
    }

    /// <summary>
    /// Registra uma rejeição no estágio, marcando-o imediatamente como rejeitado.
    /// Retorna falha se o estágio já estiver aprovado ou rejeitado.
    /// </summary>
    public Result<Unit> RecordRejection(DateTimeOffset completedAt)
    {
        if (Status == StageStatus.Approved)
            return WorkflowErrors.StageAlreadyApproved(Id.Value.ToString());

        if (Status == StageStatus.Rejected)
            return WorkflowErrors.StageAlreadyRejected(Id.Value.ToString());

        Status = StageStatus.Rejected;
        CompletedAt = completedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o estágio como ignorado (skip), sem exigir aprovação.
    /// </summary>
    public void Skip(DateTimeOffset completedAt)
    {
        Status = StageStatus.Skipped;
        CompletedAt = completedAt;
    }
}

/// <summary>Identificador fortemente tipado de WorkflowStage.</summary>
public sealed record WorkflowStageId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkflowStageId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkflowStageId From(Guid id) => new(id);
}
