using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de GovernanceDomains usando EF Core.
/// </summary>
internal sealed class GovernanceDomainRepository(GovernanceDbContext context) : IGovernanceDomainRepository
{
    public async Task<IReadOnlyList<GovernanceDomain>> ListAsync(DomainCriticality? criticality, CancellationToken ct)
    {
        var query = context.Domains.AsQueryable();

        if (criticality.HasValue)
            query = query.Where(d => d.Criticality == criticality.Value);

        return await query.OrderBy(d => d.DisplayName).ToListAsync(ct);
    }

    public async Task<GovernanceDomain?> GetByIdAsync(GovernanceDomainId id, CancellationToken ct)
        => await context.Domains.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<GovernanceDomain?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Domains.SingleOrDefaultAsync(d => d.Name == name, ct);

    public async Task AddAsync(GovernanceDomain domain, CancellationToken ct)
        => await context.Domains.AddAsync(domain, ct);

    public Task UpdateAsync(GovernanceDomain domain, CancellationToken ct)
    {
        context.Domains.Update(domain);
        return Task.CompletedTask;
    }
}
