using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de CommitAssociation — commit pool do módulo ChangeIntelligence.
/// </summary>
internal sealed class CommitAssociationRepository(ChangeIntelligenceDbContext context)
    : ICommitAssociationRepository
{
    /// <inheritdoc />
    public async Task<CommitAssociation?> GetByIdAsync(CommitAssociationId id, CancellationToken cancellationToken = default)
        => await context.CommitAssociations
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<CommitAssociation?> GetByCommitShaAndServiceAsync(
        string commitSha, string serviceName, Guid tenantId, CancellationToken cancellationToken = default)
        => await context.CommitAssociations
            .SingleOrDefaultAsync(c =>
                c.CommitSha == commitSha &&
                c.ServiceName == serviceName &&
                c.TenantId == tenantId,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommitAssociation>> ListByReleaseIdAsync(
        ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.CommitAssociations
            .Where(c => c.ReleaseId == releaseId)
            .OrderByDescending(c => c.CommittedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommitAssociation>> ListByServiceAndStatusAsync(
        string serviceName, CommitAssignmentStatus status, Guid tenantId, CancellationToken cancellationToken = default)
        => await context.CommitAssociations
            .Where(c => c.ServiceName == serviceName && c.AssignmentStatus == status && c.TenantId == tenantId)
            .OrderByDescending(c => c.CommittedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommitAssociation>> ListByServiceAndBranchAsync(
        string serviceName, string branchName, Guid tenantId, CancellationToken cancellationToken = default)
        => await context.CommitAssociations
            .Where(c => c.ServiceName == serviceName && c.BranchName == branchName && c.TenantId == tenantId)
            .OrderByDescending(c => c.CommittedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(CommitAssociation commit)
        => context.CommitAssociations.Add(commit);
}
