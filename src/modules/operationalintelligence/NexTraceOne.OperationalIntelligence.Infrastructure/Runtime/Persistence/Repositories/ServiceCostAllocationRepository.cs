using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

internal sealed class ServiceCostAllocationRepository(RuntimeIntelligenceDbContext context)
    : IServiceCostAllocationRepository
{
    public async Task<ServiceCostAllocationRecord?> GetByIdAsync(ServiceCostAllocationRecordId id, CancellationToken ct = default)
        => await context.ServiceCostAllocations.FindAsync([id.Value], ct);

    public async Task<IReadOnlyList<ServiceCostAllocationRecord>> ListByServiceAsync(
        string tenantId, string serviceName, DateTimeOffset since, DateTimeOffset until, CancellationToken ct = default)
        => await context.ServiceCostAllocations
            .Where(r => r.TenantId == tenantId
                        && r.ServiceName == serviceName
                        && r.PeriodStart >= since
                        && r.PeriodEnd <= until)
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceCostAllocationRecord>> ListByTenantAsync(
        string tenantId, DateTimeOffset since, DateTimeOffset until,
        string? environment = null, CostCategory? category = null, CancellationToken ct = default)
    {
        var query = context.ServiceCostAllocations
            .Where(r => r.TenantId == tenantId
                        && r.PeriodStart >= since
                        && r.PeriodEnd <= until);

        if (environment is not null)
            query = query.Where(r => r.Environment == environment);

        if (category.HasValue)
            query = query.Where(r => r.Category == category.Value);

        return await query.OrderByDescending(r => r.PeriodStart).ToListAsync(ct);
    }

    public void Add(ServiceCostAllocationRecord record)
        => context.ServiceCostAllocations.Add(record);
}
