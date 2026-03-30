using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de regras de matching automático.
/// </summary>
internal sealed class DiscoveryMatchRuleRepository(CatalogGraphDbContext dbContext) : IDiscoveryMatchRuleRepository
{
    public async Task<DiscoveryMatchRule?> GetByIdAsync(DiscoveryMatchRuleId id, CancellationToken cancellationToken)
        => await dbContext.DiscoveryMatchRules
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DiscoveryMatchRule>> ListActiveAsync(CancellationToken cancellationToken)
        => await dbContext.DiscoveryMatchRules
            .Where(x => x.IsActive)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DiscoveryMatchRule>> ListAllAsync(CancellationToken cancellationToken)
        => await dbContext.DiscoveryMatchRules
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public void Add(DiscoveryMatchRule rule)
        => dbContext.DiscoveryMatchRules.Add(rule);

    public void Remove(DiscoveryMatchRule rule)
        => dbContext.DiscoveryMatchRules.Remove(rule);
}
