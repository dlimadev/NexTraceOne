using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

internal sealed class WasteSignalRepository(CostIntelligenceDbContext db) : IWasteSignalRepository
{
    public async Task<IReadOnlyList<WasteSignal>> ListByServiceAsync(string serviceName, string? environment = null, CancellationToken ct = default)
    {
        var q = db.WasteSignals.Where(s => s.ServiceName == serviceName);
        if (environment is not null)
            q = q.Where(s => s.Environment == environment);
        return await q.OrderByDescending(s => s.DetectedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WasteSignal>> ListAllAsync(string? teamName = null, bool includeAcknowledged = false, CancellationToken ct = default)
    {
        var q = db.WasteSignals.AsQueryable();
        if (teamName is not null)
            q = q.Where(s => s.TeamName == teamName);
        if (!includeAcknowledged)
            q = q.Where(s => !s.IsAcknowledged);
        return await q.OrderByDescending(s => s.EstimatedMonthlySavings).ToListAsync(ct);
    }

    public async Task AddAsync(WasteSignal signal, CancellationToken ct = default)
        => await db.WasteSignals.AddAsync(signal, ct);

    public async Task<WasteSignal?> GetByIdAsync(WasteSignalId id, CancellationToken ct = default)
        => await db.WasteSignals.FindAsync([id], ct);
}
