using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de solicitações Break Glass.
/// </summary>
internal sealed class BreakGlassRepository(IdentityDbContext dbContext) : IBreakGlassRepository
{
    public async Task<BreakGlassRequest?> GetByIdAsync(BreakGlassRequestId id, CancellationToken cancellationToken)
        => await dbContext.BreakGlassRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> CountQuarterlyUsageAsync(UserId userId, DateTimeOffset quarterStart, CancellationToken cancellationToken)
        => await dbContext.BreakGlassRequests
            .CountAsync(x => x.RequestedBy == userId && x.RequestedAt >= quarterStart, cancellationToken);

    public async Task<IReadOnlyList<BreakGlassRequest>> ListActiveByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.BreakGlassRequests
            .Where(x => x.TenantId == tenantId && x.Status == BreakGlassStatus.Active)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BreakGlassRequest>> ListPendingPostMortemAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.BreakGlassRequests
            .Where(x => x.TenantId == tenantId
                && (x.Status == BreakGlassStatus.Expired || x.Status == BreakGlassStatus.Revoked)
                && x.PostMortemNotes == null)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public void Add(BreakGlassRequest request)
        => dbContext.BreakGlassRequests.Add(request);
}
