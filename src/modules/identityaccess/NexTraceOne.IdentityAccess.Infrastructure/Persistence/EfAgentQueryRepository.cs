using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence;

internal sealed class EfAgentQueryRepository(IdentityDbContext db)
    : IAgentQueryRepository
{
    public async Task AddAsync(AgentQueryRecord record, CancellationToken ct)
        => await db.AgentQueryRecords.AddAsync(record, ct);

    public async Task<IReadOnlyList<AgentQueryRecord>> ListByTokenAsync(
        Guid tokenId, int limit, CancellationToken ct)
        => await db.AgentQueryRecords
            .Where(r => r.TokenId == tokenId)
            .OrderByDescending(r => r.ExecutedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AgentQueryRecord>> ListByTenantAsync(
        Guid tenantId, DateTimeOffset since, int limit, CancellationToken ct)
        => await db.AgentQueryRecords
            .Where(r => r.TenantId == tenantId && r.ExecutedAt >= since)
            .OrderByDescending(r => r.ExecutedAt)
            .Take(limit)
            .ToListAsync(ct);
}
