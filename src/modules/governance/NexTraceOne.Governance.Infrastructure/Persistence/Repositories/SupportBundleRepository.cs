using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de bundles de suporte.</summary>
internal sealed class SupportBundleRepository(GovernanceDbContext context) : ISupportBundleRepository
{
    public async Task<IReadOnlyList<SupportBundle>> ListAsync(Guid? tenantId, CancellationToken ct)
    {
        var query = context.SupportBundles.AsNoTracking();

        if (tenantId.HasValue)
            query = query.Where(b => b.TenantId == tenantId);

        return await query
            .OrderByDescending(b => b.RequestedAt)
            .ToListAsync(ct);
    }

    public async Task<SupportBundle?> GetByIdAsync(SupportBundleId id, CancellationToken ct)
        => await context.SupportBundles
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(SupportBundle bundle, CancellationToken ct)
        => await context.SupportBundles.AddAsync(bundle, ct);

    public void Update(SupportBundle bundle)
        => context.SupportBundles.Update(bundle);
}
