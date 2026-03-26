using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>Repositório EF Core para SlaDefinition.</summary>
internal sealed class SlaDefinitionRepository(ReliabilityDbContext context) : ISlaDefinitionRepository
{
    public async Task<SlaDefinition?> GetByIdAsync(SlaDefinitionId id, Guid tenantId, CancellationToken ct)
        => await context.SlaDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<SlaDefinition>> GetBySloAsync(SloDefinitionId sloId, Guid tenantId, CancellationToken ct)
        => await context.SlaDefinitions
            .AsNoTracking()
            .Where(s => s.SloDefinitionId == sloId && s.TenantId == tenantId)
            .OrderBy(s => s.EffectiveFrom)
            .ToListAsync(ct);

    public async Task AddAsync(SlaDefinition sla, CancellationToken ct)
    {
        await context.SlaDefinitions.AddAsync(sla, ct);
        await context.CommitAsync(ct);
    }
}
