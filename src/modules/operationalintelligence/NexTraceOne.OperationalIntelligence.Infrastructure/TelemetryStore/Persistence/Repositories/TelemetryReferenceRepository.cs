using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class TelemetryReferenceRepository(TelemetryStoreDbContext context)
    : ITelemetryReferenceWriter, ITelemetryReferenceReader
{
    public async Task WriteAsync(TelemetryReference reference, CancellationToken cancellationToken = default)
    {
        context.TelemetryReferences.Add(reference);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task WriteBatchAsync(
        IReadOnlyList<TelemetryReference> references,
        CancellationToken cancellationToken = default)
    {
        context.TelemetryReferences.AddRange(references);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TelemetryReference>> GetByCorrelationAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        return await context.TelemetryReferences
            .AsNoTracking()
            .Where(r => r.CorrelationId == correlationId)
            .OrderByDescending(r => r.OriginalTimestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TelemetryReference>> GetByServiceAsync(
        Guid serviceId,
        TelemetrySignalType signalType,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return await context.TelemetryReferences
            .AsNoTracking()
            .Where(r => r.ServiceId == serviceId
                        && r.SignalType == signalType
                        && r.OriginalTimestamp >= from
                        && r.OriginalTimestamp <= until)
            .OrderBy(r => r.OriginalTimestamp)
            .ToListAsync(cancellationToken);
    }
}
