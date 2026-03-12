using NexTraceOne.Workflow.Domain.Entities;

namespace NexTraceOne.Workflow.Application.Abstractions;

/// <summary>Contrato de repositório para a entidade ApprovalDecision.</summary>
public interface IApprovalDecisionRepository
{
    /// <summary>Busca uma ApprovalDecision pelo seu identificador.</summary>
    Task<ApprovalDecision?> GetByIdAsync(ApprovalDecisionId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova ApprovalDecision ao repositório.</summary>
    void Add(ApprovalDecision decision);

    /// <summary>Lista todas as decisões de um estágio de workflow.</summary>
    Task<IReadOnlyList<ApprovalDecision>> ListByStageIdAsync(WorkflowStageId stageId, CancellationToken cancellationToken = default);

    /// <summary>Lista todas as decisões de uma instância de workflow.</summary>
    Task<IReadOnlyList<ApprovalDecision>> ListByInstanceIdAsync(WorkflowInstanceId instanceId, CancellationToken cancellationToken = default);
}
