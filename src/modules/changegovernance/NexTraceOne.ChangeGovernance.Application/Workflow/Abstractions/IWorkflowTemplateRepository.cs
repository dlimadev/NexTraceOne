using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;

/// <summary>Contrato de repositório para a entidade WorkflowTemplate.</summary>
public interface IWorkflowTemplateRepository
{
    /// <summary>Busca um WorkflowTemplate pelo seu identificador.</summary>
    Task<WorkflowTemplate?> GetByIdAsync(WorkflowTemplateId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo WorkflowTemplate ao repositório.</summary>
    void Add(WorkflowTemplate template);

    /// <summary>Atualiza um WorkflowTemplate existente.</summary>
    void Update(WorkflowTemplate template);

    /// <summary>Busca templates ativos pelo tipo de mudança.</summary>
    Task<IReadOnlyList<WorkflowTemplate>> GetByChangeTypeAsync(string changeType, CancellationToken cancellationToken = default);

    /// <summary>Lista templates ativos com paginação.</summary>
    Task<IReadOnlyList<WorkflowTemplate>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Conta o total de templates ativos.</summary>
    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);
}
