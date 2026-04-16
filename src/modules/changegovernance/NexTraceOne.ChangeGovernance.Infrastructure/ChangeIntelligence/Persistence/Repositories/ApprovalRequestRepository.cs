using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de ReleaseApprovalRequest — pedidos de aprovação de releases.
/// </summary>
internal sealed class ApprovalRequestRepository(ChangeIntelligenceDbContext context)
    : IApprovalRequestRepository
{
    /// <inheritdoc />
    public async Task<ReleaseApprovalRequest?> GetByIdAsync(
        ReleaseApprovalRequestId id, CancellationToken cancellationToken = default)
        => await context.ApprovalRequests
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseApprovalRequest>> ListPendingByReleaseIdAsync(
        ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ApprovalRequests
            .Where(a => a.ReleaseId == releaseId &&
                        a.Status == Domain.ChangeIntelligence.Enums.ApprovalRequestStatus.Pending)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseApprovalRequest>> ListByReleaseIdAsync(
        ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.ApprovalRequests
            .Where(a => a.ReleaseId == releaseId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ReleaseApprovalRequest?> GetByCallbackTokenHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
        => await context.ApprovalRequests
            .SingleOrDefaultAsync(a => a.CallbackTokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    public void Add(ReleaseApprovalRequest request)
        => context.ApprovalRequests.Add(request);
}
