using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

public interface IPresenceSessionRepository
{
    Task AddAsync(PresenceSession session, CancellationToken ct = default);
    Task<PresenceSession?> GetActiveAsync(string tenantId, string resourceType, Guid resourceId, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<PresenceSession>> ListActiveAsync(string tenantId, string resourceType, Guid resourceId, CancellationToken ct = default);
    Task SaveAsync(PresenceSession session, CancellationToken ct = default);
    Task ExpireStaleAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
