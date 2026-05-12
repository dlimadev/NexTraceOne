using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

/// <summary>Repositório EF Core de CarbonScoreRecord.</summary>
internal sealed class CarbonScoreRepository(CostIntelligenceDbContext db) : ICarbonScoreRepository
{
    public async Task<IReadOnlyList<CarbonScoreRecord>> ListByTenantAndPeriodAsync(
        Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        => await db.CarbonScoreRecords
            .Where(r => r.TenantId == tenantId && r.Date >= from && r.Date <= to && !r.IsDeleted)
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CarbonScoreRecord>> ListByServiceAsync(
        Guid serviceId, Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        => await db.CarbonScoreRecords
            .Where(r => r.ServiceId == serviceId && r.TenantId == tenantId
                        && r.Date >= from && r.Date <= to && !r.IsDeleted)
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);

    public async Task UpsertAsync(CarbonScoreRecord record, CancellationToken cancellationToken)
    {
        var existing = await db.CarbonScoreRecords
            .FirstOrDefaultAsync(r =>
                r.ServiceId == record.ServiceId &&
                r.TenantId == record.TenantId &&
                r.Date == record.Date, cancellationToken);

        if (existing is null)
            await db.CarbonScoreRecords.AddAsync(record, cancellationToken);
        else
            db.Entry(existing).CurrentValues.SetValues(record);
    }
}
