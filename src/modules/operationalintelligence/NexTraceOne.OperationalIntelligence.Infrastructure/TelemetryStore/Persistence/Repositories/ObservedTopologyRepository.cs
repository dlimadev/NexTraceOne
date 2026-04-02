using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class ObservedTopologyRepository(TelemetryStoreDbContext context)
    : IObservedTopologyWriter, IObservedTopologyReader
{
    public async Task UpsertAsync(ObservedTopologyEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await context.ObservedTopologyEntries
            .FirstOrDefaultAsync(e =>
                    e.SourceServiceId == entry.SourceServiceId
                    && e.TargetServiceId == entry.TargetServiceId
                    && e.Environment == entry.Environment
                    && e.CommunicationType == entry.CommunicationType,
                cancellationToken);

        if (existing is not null)
        {
            context.Entry(existing).CurrentValues.SetValues(entry);
        }
        else
        {
            context.ObservedTopologyEntries.Add(entry);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertBatchAsync(
        IReadOnlyList<ObservedTopologyEntry> entries,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var sourceIds = entries.Select(e => e.SourceServiceId).Distinct().ToList();
        var targetIds = entries.Select(e => e.TargetServiceId).Distinct().ToList();
        var environments = entries.Select(e => e.Environment).Distinct().ToList();

        var existingEntries = await context.ObservedTopologyEntries
            .Where(e => sourceIds.Contains(e.SourceServiceId)
                        && targetIds.Contains(e.TargetServiceId)
                        && environments.Contains(e.Environment))
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            var existing = existingEntries.FirstOrDefault(e =>
                e.SourceServiceId == entry.SourceServiceId
                && e.TargetServiceId == entry.TargetServiceId
                && e.Environment == entry.Environment
                && e.CommunicationType == entry.CommunicationType);

            if (existing is not null)
            {
                context.Entry(existing).CurrentValues.SetValues(entry);
            }
            else
            {
                context.ObservedTopologyEntries.Add(entry);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObservedTopologyEntry>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await context.ObservedTopologyEntries
            .AsNoTracking()
            .Where(e => (e.SourceServiceId == serviceId || e.TargetServiceId == serviceId)
                        && e.Environment == environment)
            .OrderByDescending(e => e.LastSeenAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObservedTopologyEntry>> GetByEnvironmentAsync(
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await context.ObservedTopologyEntries
            .AsNoTracking()
            .Where(e => e.Environment == environment)
            .OrderByDescending(e => e.LastSeenAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObservedTopologyEntry>> GetShadowDependenciesAsync(
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await context.ObservedTopologyEntries
            .AsNoTracking()
            .Where(e => e.Environment == environment && e.IsShadowDependency)
            .OrderByDescending(e => e.LastSeenAt)
            .ToListAsync(cancellationToken);
    }
}
