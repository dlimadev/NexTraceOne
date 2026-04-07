using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de FeatureFlagDefinition e FeatureFlagEntry usando EF Core.
/// </summary>
internal sealed class FeatureFlagRepository(ConfigurationDbContext context)
    : IFeatureFlagRepository
{
    // ── FeatureFlagDefinition ──────────────────────────────────────────

    public async Task<FeatureFlagDefinition?> GetDefinitionByKeyAsync(
        string key,
        CancellationToken cancellationToken)
        => await context.FeatureFlagDefinitions
            .SingleOrDefaultAsync(d => d.Key == key, cancellationToken);

    public async Task<IReadOnlyList<FeatureFlagDefinition>> GetAllDefinitionsAsync(
        CancellationToken cancellationToken)
        => await context.FeatureFlagDefinitions
            .OrderBy(d => d.Key)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddDefinitionAsync(
        FeatureFlagDefinition definition,
        CancellationToken cancellationToken)
        => await context.FeatureFlagDefinitions.AddAsync(definition, cancellationToken);

    public Task UpdateDefinitionAsync(
        FeatureFlagDefinition definition,
        CancellationToken cancellationToken)
    {
        context.FeatureFlagDefinitions.Update(definition);
        return Task.CompletedTask;
    }

    // ── FeatureFlagEntry ───────────────────────────────────────────────

    public async Task<FeatureFlagEntry?> GetEntryByKeyAndScopeAsync(
        string key,
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken)
        => await context.FeatureFlagEntries
            .SingleOrDefaultAsync(
                e => e.Key == key && e.Scope == scope && e.ScopeReferenceId == scopeReferenceId,
                cancellationToken);

    public async Task<IReadOnlyList<FeatureFlagEntry>> GetAllEntriesByKeyAsync(
        string key,
        CancellationToken cancellationToken)
        => await context.FeatureFlagEntries
            .Where(e => e.Key == key)
            .OrderBy(e => e.Scope)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddEntryAsync(
        FeatureFlagEntry entry,
        CancellationToken cancellationToken)
        => await context.FeatureFlagEntries.AddAsync(entry, cancellationToken);

    public Task UpdateEntryAsync(
        FeatureFlagEntry entry,
        CancellationToken cancellationToken)
    {
        context.FeatureFlagEntries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteEntryAsync(
        FeatureFlagEntry entry,
        CancellationToken cancellationToken)
    {
        context.FeatureFlagEntries.Remove(entry);
        return Task.CompletedTask;
    }
}
