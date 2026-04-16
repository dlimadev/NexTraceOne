using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ConfigurationEntry usando EF Core.
/// </summary>
internal sealed class ConfigurationEntryRepository(ConfigurationDbContext context)
    : IConfigurationEntryRepository
{
    public async Task<ConfigurationEntry?> GetByIdAsync(ConfigurationEntryId id, CancellationToken cancellationToken)
        => await context.Entries.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<ConfigurationEntry?> GetByKeyAndScopeAsync(
        string key,
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken)
        => await context.Entries.SingleOrDefaultAsync(
            e => e.Key == key && e.Scope == scope && e.ScopeReferenceId == scopeReferenceId,
            cancellationToken);

    public async Task<IReadOnlyList<ConfigurationEntry>> GetAllByScopeAsync(
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken)
        => await context.Entries
            .Where(e => e.Scope == scope && e.ScopeReferenceId == scopeReferenceId)
            .OrderBy(e => e.Key)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConfigurationEntry>> GetAllByScopeWithKeyPrefixAsync(
        ConfigurationScope scope,
        string? scopeReferenceId,
        string? keyPrefix,
        CancellationToken cancellationToken)
    {
        var query = context.Entries.Where(e => e.Scope == scope && e.ScopeReferenceId == scopeReferenceId);
        if (!string.IsNullOrWhiteSpace(keyPrefix))
            query = query.Where(e => e.Key.StartsWith(keyPrefix));
        return await query.OrderBy(e => e.Key).AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationEntry>> GetAllByKeyAsync(string key, CancellationToken cancellationToken)
        => await context.Entries
            .Where(e => e.Key == key)
            .OrderBy(e => e.Scope)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ConfigurationEntry entry, CancellationToken cancellationToken)
    {
        await context.Entries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ConfigurationEntry entry, CancellationToken cancellationToken)
    {
        context.Entries.Update(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ConfigurationEntry entry, CancellationToken cancellationToken)
    {
        context.Entries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
