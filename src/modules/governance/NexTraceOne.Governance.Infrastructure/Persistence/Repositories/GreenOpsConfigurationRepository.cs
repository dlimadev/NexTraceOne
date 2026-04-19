using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de configuração GreenOps.</summary>
internal sealed class GreenOpsConfigurationRepository(GovernanceDbContext context) : IGreenOpsConfigurationRepository
{
    public async Task<GreenOpsConfiguration?> GetActiveAsync(Guid? tenantId, CancellationToken ct)
        => await context.GreenOpsConfigurations
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(GreenOpsConfiguration config, CancellationToken ct)
        => await context.GreenOpsConfigurations.AddAsync(config, ct);

    public void Update(GreenOpsConfiguration config)
        => context.GreenOpsConfigurations.Update(config);
}
