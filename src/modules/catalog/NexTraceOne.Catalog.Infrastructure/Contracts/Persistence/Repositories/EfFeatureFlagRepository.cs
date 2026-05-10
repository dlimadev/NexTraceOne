using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de FeatureFlagRecord.
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance.
/// </summary>
internal sealed class EfFeatureFlagRepository(ContractsDbContext context) : IFeatureFlagRepository
{
    public async Task UpsertAsync(FeatureFlagRecord record, CancellationToken ct)
    {
        var existing = await context.FeatureFlagRecords
            .Where(f => f.TenantId == record.TenantId
                     && f.ServiceId == record.ServiceId
                     && f.FlagKey == record.FlagKey)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            context.FeatureFlagRecords.Add(record);
        }
        else
        {
            existing.Upsert(
                record.IsEnabled,
                record.EnabledEnvironmentsJson,
                record.OwnerId,
                record.LastToggledAt,
                record.ScheduledRemovalDate,
                DateTimeOffset.UtcNow);
        }
    }

    public async Task<IReadOnlyList<FeatureFlagRecord>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
        => await context.FeatureFlagRecords
            .Where(f => f.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FeatureFlagRecord>> ListByServiceAsync(
        string serviceId, string tenantId, CancellationToken ct)
        => await context.FeatureFlagRecords
            .Where(f => f.ServiceId == serviceId && f.TenantId == tenantId)
            .ToListAsync(ct);
}
