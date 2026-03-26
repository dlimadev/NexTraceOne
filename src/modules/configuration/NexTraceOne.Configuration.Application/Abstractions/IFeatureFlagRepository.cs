using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Repository abstraction for FeatureFlagDefinition and FeatureFlagEntry aggregates.
/// Provides read and write operations for feature flag definitions and per-scope overrides.
/// </summary>
public interface IFeatureFlagRepository
{
    // ── FeatureFlagDefinition ──────────────────────────────────────────

    /// <summary>Returns the flag definition matching the given unique key, or null if not found.</summary>
    Task<FeatureFlagDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>Returns all registered feature flag definitions, ordered by key.</summary>
    Task<IReadOnlyList<FeatureFlagDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken);

    /// <summary>Persists a new feature flag definition.</summary>
    Task AddDefinitionAsync(FeatureFlagDefinition definition, CancellationToken cancellationToken);

    /// <summary>Persists changes to an existing feature flag definition.</summary>
    Task UpdateDefinitionAsync(FeatureFlagDefinition definition, CancellationToken cancellationToken);

    // ── FeatureFlagEntry ───────────────────────────────────────────────

    /// <summary>Returns the entry for the given key, scope, and optional scope reference, or null.</summary>
    Task<FeatureFlagEntry?> GetEntryByKeyAndScopeAsync(
        string key,
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken);

    /// <summary>Returns all active entries for the given flag key across all scopes.</summary>
    Task<IReadOnlyList<FeatureFlagEntry>> GetAllEntriesByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>Persists a new feature flag entry.</summary>
    Task AddEntryAsync(FeatureFlagEntry entry, CancellationToken cancellationToken);

    /// <summary>Persists changes to an existing feature flag entry.</summary>
    Task UpdateEntryAsync(FeatureFlagEntry entry, CancellationToken cancellationToken);

    /// <summary>Removes a feature flag entry from persistence.</summary>
    Task DeleteEntryAsync(FeatureFlagEntry entry, CancellationToken cancellationToken);
}
