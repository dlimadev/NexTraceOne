using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiUsageEntryRepository(AiGovernanceDbContext context) : IAiUsageEntryRepository
{
    public async Task<IReadOnlyList<AIUsageEntry>> ListAsync(
        string? userId, Guid? modelId, DateTimeOffset? startDate, DateTimeOffset? endDate,
        UsageResult? result, AIClientType? clientType, int pageSize, CancellationToken ct)
    {
        var query = context.UsageEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(e => e.UserId == userId);

        if (modelId.HasValue)
            query = query.Where(e => e.ModelId == modelId.Value);

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        if (result.HasValue)
            query = query.Where(e => e.Result == result.Value);

        if (clientType.HasValue)
            query = query.Where(e => e.ClientType == clientType.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AIUsageEntry entry, CancellationToken ct)
    {
        await context.UsageEntries.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
        => await context.UsageEntries.Where(e => e.Timestamp < cutoff).ExecuteDeleteAsync(ct);

    public async Task<IReadOnlyList<AiUsageAggregate>> GetAggregatedUsageAsync(
        Guid tenantId, DateTimeOffset start, DateTimeOffset end, string groupBy, int top, CancellationToken ct)
    {
        // TenantId filtering is applied when available on the entity
        var query = context.UsageEntries
            .Where(e => e.Timestamp >= start && e.Timestamp <= end)
            .AsQueryable();

        return groupBy.ToLowerInvariant() switch
        {
            "user" => await query
                .GroupBy(e => e.UserId)
                .OrderByDescending(g => g.Sum(e => (long)e.TotalTokens))
                .Take(top)
                .Select(g => new AiUsageAggregate(
                    g.Key,
                    g.Key,
                    g.Sum(e => (long)e.TotalTokens),
                    g.Count(),
                    null))
                .ToListAsync(ct),

            "provider" => await query
                .GroupBy(e => e.Provider)
                .OrderByDescending(g => g.Sum(e => (long)e.TotalTokens))
                .Take(top)
                .Select(g => new AiUsageAggregate(
                    g.Key,
                    g.Key,
                    g.Sum(e => (long)e.TotalTokens),
                    g.Count(),
                    null))
                .ToListAsync(ct),

            _ => await query
                .GroupBy(e => e.ModelId)
                .OrderByDescending(g => g.Sum(e => (long)e.TotalTokens))
                .Take(top)
                .Select(g => new AiUsageAggregate(
                    g.Key.ToString(),
                    g.Key.ToString(),
                    g.Sum(e => (long)e.TotalTokens),
                    g.Count(),
                    null))
                .ToListAsync(ct),
        };
    }
}
