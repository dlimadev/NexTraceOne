using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;

namespace NexTraceOne.Workflow.Application.Abstractions;

/// <summary>Contrato de repositório para a entidade WorkflowInstance.</summary>
public interface IWorkflowInstanceRepository
{
    /// <summary>Busca uma WorkflowInstance pelo seu identificador.</summary>
    Task<WorkflowInstance?> GetByIdAsync(WorkflowInstanceId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova WorkflowInstance ao repositório.</summary>
    void Add(WorkflowInstance instance);

    /// <summary>Atualiza uma WorkflowInstance existente.</summary>
    void Update(WorkflowInstance instance);

    /// <summary>Busca instância de workflow pela release associada.</summary>
    Task<WorkflowInstance?> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default);

    /// <summary>Lista instâncias de workflow por status com paginação.</summary>
    Task<IReadOnlyList<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Conta o total de instâncias de workflow com o status informado.</summary>
    Task<int> CountByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
}
