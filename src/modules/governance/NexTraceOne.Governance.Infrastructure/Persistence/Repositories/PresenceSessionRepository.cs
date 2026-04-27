using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

public sealed class PresenceSessionRepository(GovernanceDbContext context) : IPresenceSessionRepository
{
    public async Task AddAsync(PresenceSession session, CancellationToken ct = default)
        => await context.PresenceSessions.AddAsync(session, ct);

    public async Task<PresenceSession?> GetActiveAsync(string tenantId, string resourceType, Guid resourceId, string userId, CancellationToken ct = default)
        => await context.PresenceSessions
            .Where(s => s.TenantId == tenantId && s.ResourceType == resourceType && s.ResourceId == resourceId && s.UserId == userId && s.IsActive)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PresenceSession>> ListActiveAsync(string tenantId, string resourceType, Guid resourceId, CancellationToken ct = default)
        => await context.PresenceSessions
            .Where(s => s.TenantId == tenantId && s.ResourceType == resourceType && s.ResourceId == resourceId && s.IsActive)
            .OrderBy(s => s.JoinedAt)
            .ToListAsync(ct);

    public Task SaveAsync(PresenceSession session, CancellationToken ct = default)
    {
        context.PresenceSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task ExpireStaleAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        var stale = await context.PresenceSessions
            .Where(s => s.IsActive && s.LastSeenAt < cutoff)
            .ToListAsync(ct);
        foreach (var s in stale) s.Leave(cutoff);
    }
}
