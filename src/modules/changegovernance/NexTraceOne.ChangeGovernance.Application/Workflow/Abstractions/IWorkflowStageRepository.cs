using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;

/// <summary>Contrato de repositório para a entidade WorkflowStage.</summary>
public interface IWorkflowStageRepository
{
    /// <summary>Busca um WorkflowStage pelo seu identificador.</summary>
    Task<WorkflowStage?> GetByIdAsync(WorkflowStageId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo WorkflowStage ao repositório.</summary>
    void Add(WorkflowStage stage);

    /// <summary>Atualiza um WorkflowStage existente.</summary>
    void Update(WorkflowStage stage);

    /// <summary>Lista todos os estágios de uma instância de workflow ordenados pela ordem sequencial.</summary>
    Task<IReadOnlyList<WorkflowStage>> ListByInstanceIdAsync(WorkflowInstanceId instanceId, CancellationToken cancellationToken = default);
}
