using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para a entidade WorkItemAssociation.</summary>
public interface IWorkItemAssociationRepository
{
    /// <summary>Busca uma WorkItemAssociation pelo seu identificador.</summary>
    Task<WorkItemAssociation?> GetByIdAsync(WorkItemAssociationId id, CancellationToken cancellationToken = default);

    /// <summary>Lista work items activos de uma release.</summary>
    Task<IReadOnlyList<WorkItemAssociation>> ListActiveByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Lista todos os work items de uma release (incluindo removidos).</summary>
    Task<IReadOnlyList<WorkItemAssociation>> ListAllByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Verifica se um work item externo já está activo numa release.</summary>
    Task<bool> ExistsActiveAsync(ReleaseId releaseId, string externalWorkItemId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova WorkItemAssociation.</summary>
    void Add(WorkItemAssociation workItem);
}
