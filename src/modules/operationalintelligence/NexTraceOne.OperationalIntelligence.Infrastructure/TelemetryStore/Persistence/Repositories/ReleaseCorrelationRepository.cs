using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class ReleaseCorrelationRepository(TelemetryStoreDbContext context)
    : IReleaseCorrelationWriter, IReleaseCorrelationReader
{
    public async Task WriteAsync(ReleaseRuntimeCorrelation correlation, CancellationToken cancellationToken = default)
    {
        context.ReleaseRuntimeCorrelations.Add(correlation);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReleaseRuntimeCorrelation>> GetByReleaseAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        return await context.ReleaseRuntimeCorrelations
            .AsNoTracking()
            .Where(c => c.ReleaseId == releaseId)
            .OrderByDescending(c => c.DeployedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReleaseRuntimeCorrelation>> GetByServiceAsync(
        Guid serviceId,
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return await context.ReleaseRuntimeCorrelations
            .AsNoTracking()
            .Where(c => c.ServiceId == serviceId
                        && c.Environment == environment
                        && c.DeployedAt >= from
                        && c.DeployedAt <= until)
            .OrderByDescending(c => c.DeployedAt)
            .ToListAsync(cancellationToken);
    }
}
