using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de solicitações JIT Access.
/// </summary>
internal sealed class JitAccessRepository(IdentityDbContext dbContext) : IJitAccessRepository
{
    public async Task<JitAccessRequest?> GetByIdAsync(JitAccessRequestId id, CancellationToken cancellationToken)
        => await dbContext.JitAccessRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<JitAccessRequest>> ListPendingByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.JitAccessRequests
            .Where(x => x.TenantId == tenantId && x.Status == JitAccessStatus.Pending)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JitAccessRequest>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.JitAccessRequests
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JitAccessRequest>> ListByUserAsync(UserId userId, CancellationToken cancellationToken)
        => await dbContext.JitAccessRequests
            .Where(x => x.RequestedBy == userId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasActiveGrantAsync(UserId userId, string permissionCode, DateTimeOffset now, CancellationToken cancellationToken)
        => await dbContext.JitAccessRequests
            .AnyAsync(x => x.RequestedBy == userId
                && x.PermissionCode == permissionCode
                && x.Status == JitAccessStatus.Approved
                && x.GrantedFrom != null && x.GrantedFrom <= now
                && x.GrantedUntil != null && x.GrantedUntil > now,
                cancellationToken);

    public void Add(JitAccessRequest request)
        => dbContext.JitAccessRequests.Add(request);
}
