using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Observability.Telemetry.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Models;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence.Repositories;

internal sealed class InvestigationContextRepository(TelemetryStoreDbContext context)
    : IInvestigationContextWriter, IInvestigationContextReader
{
    public async Task UpsertAsync(InvestigationContext investigationContext, CancellationToken cancellationToken = default)
    {
        var existing = await context.InvestigationContexts.FindAsync([investigationContext.Id], cancellationToken);

        if (existing is not null)
        {
            context.Entry(existing).CurrentValues.SetValues(investigationContext);
        }
        else
        {
            context.InvestigationContexts.Add(investigationContext);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<InvestigationContext?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.InvestigationContexts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InvestigationContext>> GetOpenByServiceAsync(
        Guid serviceId,
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await context.InvestigationContexts
            .AsNoTracking()
            .Where(c => c.PrimaryServiceId == serviceId
                        && c.Environment == environment
                        && c.Status != "resolved"
                        && c.Status != "dismissed")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvestigationContext>> GetByTimeRangeAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return await context.InvestigationContexts
            .AsNoTracking()
            .Where(c => c.Environment == environment
                        && c.TimeWindowStart <= until
                        && c.TimeWindowEnd >= from)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
