using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de WorkItemAssociation — associações de work items a releases.
/// </summary>
internal sealed class WorkItemAssociationRepository(ChangeIntelligenceDbContext context)
    : IWorkItemAssociationRepository
{
    /// <inheritdoc />
    public async Task<WorkItemAssociation?> GetByIdAsync(WorkItemAssociationId id, CancellationToken cancellationToken = default)
        => await context.WorkItemAssociations
            .SingleOrDefaultAsync(wi => wi.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkItemAssociation>> ListActiveByReleaseIdAsync(
        ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.WorkItemAssociations
            .Where(wi => wi.ReleaseId == releaseId && wi.IsActive)
            .OrderBy(wi => wi.AddedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkItemAssociation>> ListAllByReleaseIdAsync(
        ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.WorkItemAssociations
            .Where(wi => wi.ReleaseId == releaseId)
            .OrderBy(wi => wi.AddedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsActiveAsync(
        ReleaseId releaseId, string externalWorkItemId, CancellationToken cancellationToken = default)
        => await context.WorkItemAssociations
            .AnyAsync(wi =>
                wi.ReleaseId == releaseId &&
                wi.ExternalWorkItemId == externalWorkItemId &&
                wi.IsActive,
            cancellationToken);

    /// <inheritdoc />
    public void Add(WorkItemAssociation workItem)
        => context.WorkItemAssociations.Add(workItem);
}
