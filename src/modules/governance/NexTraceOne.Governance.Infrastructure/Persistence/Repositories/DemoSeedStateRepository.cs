using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de estado do seed de demonstração.</summary>
internal sealed class DemoSeedStateRepository(GovernanceDbContext context) : IDemoSeedStateRepository
{
    public async Task<DemoSeedState> GetOrCreateAsync(Guid? tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await context.DemoSeedStates
            .SingleOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (existing is not null)
            return existing;

        var created = DemoSeedState.Create(tenantId, now);
        await context.DemoSeedStates.AddAsync(created, ct);
        return created;
    }

    public void Update(DemoSeedState state)
        => context.DemoSeedStates.Update(state);
}
