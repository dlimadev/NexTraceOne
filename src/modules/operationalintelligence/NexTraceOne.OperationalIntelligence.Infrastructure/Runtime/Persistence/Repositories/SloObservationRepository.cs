using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>Repositório de observações de SLO. Wave J.2 — SLO Tracking.</summary>
internal sealed class SloObservationRepository(RuntimeIntelligenceDbContext context) : ISloObservationRepository
{
    public async Task<SloObservation?> GetByIdAsync(SloObservationId id, CancellationToken ct = default)
        => await context.SloObservations.FindAsync([id.Value], ct);

    public async Task<IReadOnlyList<SloObservation>> ListByServiceAsync(
        string tenantId,
        string serviceName,
        DateTimeOffset since,
        DateTimeOffset until,
        string? environment = null,
        CancellationToken ct = default)
    {
        var query = context.SloObservations
            .Where(o => o.TenantId == tenantId
                     && o.ServiceName == serviceName
                     && o.ObservedAt >= since
                     && o.ObservedAt <= until);

        if (environment is not null)
            query = query.Where(o => o.Environment == environment);

        return await query.OrderByDescending(o => o.ObservedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SloObservation>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        SloObservationStatus? statusFilter = null,
        CancellationToken ct = default)
    {
        var query = context.SloObservations
            .Where(o => o.TenantId == tenantId
                     && o.ObservedAt >= since
                     && o.ObservedAt <= until);

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        return await query.OrderByDescending(o => o.ObservedAt).ToListAsync(ct);
    }

    public void Add(SloObservation observation) => context.SloObservations.Add(observation);
}
