using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>Repositório EF Core para SloDefinition.</summary>
internal sealed class SloDefinitionRepository(ReliabilityDbContext context) : ISloDefinitionRepository
{
    public async Task<SloDefinition?> GetByIdAsync(SloDefinitionId id, Guid tenantId, CancellationToken ct)
        => await context.SloDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<SloDefinition>> GetByServiceAsync(string serviceId, Guid tenantId, CancellationToken ct)
        => await context.SloDefinitions
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceId && s.TenantId == tenantId)
            .OrderBy(s => s.Environment).ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SloDefinition>> GetActiveByServiceAsync(string serviceId, string environment, Guid tenantId, CancellationToken ct)
        => await context.SloDefinitions
            .AsNoTracking()
            .Where(s => s.ServiceId == serviceId && s.Environment == environment && s.TenantId == tenantId && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task AddAsync(SloDefinition slo, CancellationToken ct)
    {
        await context.SloDefinitions.AddAsync(slo, ct);
        await context.CommitAsync(ct);
    }
}
