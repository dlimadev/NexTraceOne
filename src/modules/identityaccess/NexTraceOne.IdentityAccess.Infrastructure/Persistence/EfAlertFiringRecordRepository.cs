using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence;

internal sealed class EfAlertFiringRecordRepository(IdentityDbContext context) : IAlertFiringRecordRepository
{
    public async Task<IReadOnlyList<AlertFiringRecord>> ListByTenantAsync(
        Guid tenantId,
        AlertFiringStatus? statusFilter,
        int days,
        CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-days);
        var query = context.AlertFiringRecords
            .Where(r => r.TenantId == tenantId && r.FiredAt >= since);

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        return await query.OrderByDescending(r => r.FiredAt).ToListAsync(ct);
    }

    public async Task<AlertFiringRecord?> GetByIdAsync(AlertFiringRecordId id, CancellationToken ct = default)
        => await context.AlertFiringRecords.FindAsync([id], ct);

    public async Task<bool> HasFiringAlertAsync(Guid tenantId, Guid alertRuleId, CancellationToken ct = default)
        => await context.AlertFiringRecords
            .AnyAsync(r => r.TenantId == tenantId && r.AlertRuleId == alertRuleId && r.Status == AlertFiringStatus.Firing, ct);

    public void Add(AlertFiringRecord record) => context.AlertFiringRecords.Add(record);

    public void Update(AlertFiringRecord record) => context.AlertFiringRecords.Update(record);
}
