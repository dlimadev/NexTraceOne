using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class ExternalDataSourceRepository(AiGovernanceDbContext context)
    : IExternalDataSourceRepository
{
    public async Task<IReadOnlyList<ExternalDataSource>> ListAsync(
        ExternalDataSourceConnectorType? connectorType,
        bool? isActive,
        CancellationToken ct)
    {
        var query = context.ExternalDataSources.AsQueryable();

        if (connectorType.HasValue)
            query = query.Where(s => s.ConnectorType == connectorType.Value);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        return await query
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<ExternalDataSource?> GetByIdAsync(ExternalDataSourceId id, CancellationToken ct)
        => await context.ExternalDataSources.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await context.ExternalDataSources.AnyAsync(s => s.Name == name, ct);

    public async Task AddAsync(ExternalDataSource source, CancellationToken ct)
    {
        await context.ExternalDataSources.AddAsync(source, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExternalDataSource>> ListDueForSyncAsync(
        DateTimeOffset now,
        CancellationToken ct)
    {
        return await context.ExternalDataSources
            .Where(s => s.IsActive
                && s.SyncIntervalMinutes > 0
                && (s.LastSyncedAt == null
                    || s.LastSyncedAt.Value.AddMinutes(s.SyncIntervalMinutes) <= now))
            .OrderBy(s => s.Priority)
            .ToListAsync(ct);
    }
}
