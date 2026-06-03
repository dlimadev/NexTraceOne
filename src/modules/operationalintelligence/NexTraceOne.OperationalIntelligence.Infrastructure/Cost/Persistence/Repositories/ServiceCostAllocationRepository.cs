using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Repositories;

/// <summary>
/// Repositório de registos de alocação de custo por serviço (FinOps Contextual).
/// Wave I.2 — FinOps Contextual por Serviço.
/// </summary>
internal sealed class ServiceCostAllocationRepository(IncidentResponseDbContext context)
    : RepositoryBase<ServiceCostAllocationRecord, ServiceCostAllocationRecordId>(context),
      IServiceCostAllocationRepository
{
    /// <inheritdoc />
    public override async Task<ServiceCostAllocationRecord?> GetByIdAsync(
        ServiceCostAllocationRecordId id, CancellationToken ct = default)
        => await context.ServiceCostAllocations
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceCostAllocationRecord>> ListByServiceAsync(
        string tenantId,
        string serviceName,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken ct = default)
        => await context.ServiceCostAllocations
            .Where(r => r.TenantId == tenantId
                && r.ServiceName == serviceName
                && r.PeriodStart >= since
                && r.PeriodEnd <= until)
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ServiceCostAllocationRecord>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        string? environment = null,
        CostCategory? category = null,
        CancellationToken ct = default)
    {
        var query = context.ServiceCostAllocations
            .Where(r => r.TenantId == tenantId
                && r.PeriodStart >= since
                && r.PeriodEnd <= until);

        if (environment is not null)
            query = query.Where(r => r.Environment == environment);

        if (category.HasValue)
            query = query.Where(r => r.Category == category.Value);

        return await query
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(ct);
    }
}
