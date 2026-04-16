using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de GovernanceRolloutRecords usando EF Core.
/// </summary>
internal sealed class GovernanceRolloutRecordRepository(GovernanceDbContext context) : IGovernanceRolloutRecordRepository
{
    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListAsync(
        GovernancePackId? packId,
        GovernanceScopeType? scopeType,
        string? scopeValue,
        RolloutStatus? status,
        CancellationToken ct)
    {
        var query = context.RolloutRecords.AsQueryable();

        if (packId is not null)
            query = query.Where(r => r.PackId == packId);

        if (scopeType.HasValue)
            query = query.Where(r => r.ScopeType == scopeType.Value);

        if (!string.IsNullOrWhiteSpace(scopeValue))
            query = query.Where(r => r.Scope == scopeValue);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListByPackIdAsync(
        GovernancePackId packId,
        CancellationToken ct)
        => await context.RolloutRecords
            .Where(r => r.PackId == packId)
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListByVersionIdAsync(
        GovernancePackVersionId versionId,
        CancellationToken ct)
        => await context.RolloutRecords
            .Where(r => r.VersionId == versionId)
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListByStatusAsync(
        RolloutStatus status,
        CancellationToken ct)
        => await context.RolloutRecords
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);

    public async Task<GovernanceRolloutRecord?> GetByIdAsync(GovernanceRolloutRecordId id, CancellationToken ct)
        => await context.RolloutRecords.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(GovernanceRolloutRecord record, CancellationToken ct)
        => await context.RolloutRecords.AddAsync(record, ct);

    public Task UpdateAsync(GovernanceRolloutRecord record, CancellationToken ct)
    {
        context.RolloutRecords.Update(record);
        return Task.CompletedTask;
    }
}
