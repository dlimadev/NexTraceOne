using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ConfigurationDefinition usando EF Core.
/// </summary>
internal sealed class ConfigurationDefinitionRepository(ConfigurationDbContext context)
    : IConfigurationDefinitionRepository
{
    public async Task<ConfigurationDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
        => await context.Definitions.SingleOrDefaultAsync(d => d.Key == key, cancellationToken);

    public async Task<IReadOnlyList<ConfigurationDefinition>> GetAllAsync(CancellationToken cancellationToken)
        => await context.Definitions.OrderBy(d => d.SortOrder).ThenBy(d => d.Key).AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(ConfigurationDefinition definition, CancellationToken cancellationToken)
    {
        await context.Definitions.AddAsync(definition, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ConfigurationDefinition definition, CancellationToken cancellationToken)
    {
        context.Definitions.Update(definition);
        await context.SaveChangesAsync(cancellationToken);
    }
}
