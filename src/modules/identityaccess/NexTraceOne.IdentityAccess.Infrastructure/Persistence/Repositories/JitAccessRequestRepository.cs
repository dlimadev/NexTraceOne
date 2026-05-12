using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de requests JIT.
/// </summary>
internal sealed class JitAccessRequestRepository(IdentityDbContext db)
    : IJitAccessRequestRepository
{
    /// <inheritdoc />
    public async Task AddAsync(JitAccessRequest request, CancellationToken cancellationToken)
        => await db.JitAccessRequests.AddAsync(request, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<JitAccessRequest>> ListPendingByUserAsync(
        UserId userId, TenantId tenantId, CancellationToken cancellationToken)
        => await db.JitAccessRequests
            .Where(r => r.RequestedBy == userId 
                     && r.TenantId == tenantId 
                     && r.Status == JitAccessStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<JitAccessRequest>> ListActiveByUserAsync(
        UserId userId, TenantId tenantId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.JitAccessRequests
            .Where(r => r.RequestedBy == userId 
                     && r.TenantId == tenantId 
                     && r.Status == JitAccessStatus.Approved
                     && r.GrantedFrom <= now 
                     && r.GrantedUntil > now)
            .OrderByDescending(r => r.GrantedUntil)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<JitAccessRequest?> GetByIdAsync(
        JitAccessRequestId id, CancellationToken cancellationToken)
        => await db.JitAccessRequests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
}
