using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para a entidade CommitAssociation (commit pool).</summary>
public interface ICommitAssociationRepository
{
    /// <summary>Busca uma CommitAssociation pelo seu identificador.</summary>
    Task<CommitAssociation?> GetByIdAsync(CommitAssociationId id, CancellationToken cancellationToken = default);

    /// <summary>Busca um commit pelo SHA e nome do serviço.</summary>
    Task<CommitAssociation?> GetByCommitShaAndServiceAsync(string commitSha, string serviceName, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lista commits associados a uma release.</summary>
    Task<IReadOnlyList<CommitAssociation>> ListByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Lista commits de um serviço num estado específico (ex: Candidate, Unassigned).</summary>
    Task<IReadOnlyList<CommitAssociation>> ListByServiceAndStatusAsync(string serviceName, CommitAssignmentStatus status, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lista commits de um serviço num branch específico.</summary>
    Task<IReadOnlyList<CommitAssociation>> ListByServiceAndBranchAsync(string serviceName, string branchName, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova CommitAssociation.</summary>
    void Add(CommitAssociation commit);
}
