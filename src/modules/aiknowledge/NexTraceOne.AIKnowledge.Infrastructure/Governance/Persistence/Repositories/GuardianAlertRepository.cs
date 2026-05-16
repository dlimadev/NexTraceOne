using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class GuardianAlertRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IGuardianAlertRepository
{
    public async Task<IReadOnlyList<GuardianAlert>> ListOpenAsync(Guid tenantId, CancellationToken ct)
        => await context.GuardianAlerts
            .Where(a => a.TenantId == tenantId && a.Status == "open")
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GuardianAlert>> ListByServiceAsync(string serviceName, Guid tenantId, CancellationToken ct)
        => await context.GuardianAlerts
            .Where(a => a.ServiceName == serviceName && a.TenantId == tenantId)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GuardianAlert>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
        => await context.GuardianAlerts
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.DetectedAt)
            .ToListAsync(ct);

    public async Task<GuardianAlert?> GetByIdAsync(GuardianAlertId id, CancellationToken ct)
        => await context.GuardianAlerts.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(a => a.Id == id, ct);

    public void Add(GuardianAlert alert) => context.GuardianAlerts.Add(alert);
}
