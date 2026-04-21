using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiTokenUsageLedgerRepository(AiGovernanceDbContext context) : IAiTokenUsageLedgerRepository
{
    public async Task AddAsync(AiTokenUsageLedger entity, CancellationToken ct)
        => await context.TokenUsageLedger.AddAsync(entity, ct);

    public async Task<IReadOnlyList<AiTokenUsageLedger>> GetByUserAsync(string userId, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenUsageLedger>> GetByTenantAsync(Guid tenantId, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);

    public async Task<long> GetTotalTokensForPeriodAsync(
        string userId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.UserId == userId && e.Timestamp >= start && e.Timestamp <= end && !e.IsBlocked)
            .SumAsync(e => (long)e.TotalTokens, ct);

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
        => await context.TokenUsageLedger.Where(e => e.Timestamp < cutoff).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<AiTokenUsageLedger>> ListByPeriodAsync(DateTimeOffset cutoff, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.Timestamp >= cutoff)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);
}
