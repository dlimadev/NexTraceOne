using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Repository abstraction for ConfigurationEntry aggregate.
/// Provides read and write operations for concrete configuration values
/// bound to specific scopes within the platform.
/// </summary>
public interface IConfigurationEntryRepository
{
    /// <summary>Returns the entry matching the given identifier, or null if not found.</summary>
    Task<ConfigurationEntry?> GetByIdAsync(ConfigurationEntryId id, CancellationToken cancellationToken);

    /// <summary>Returns the entry matching the given key, scope, and optional scope reference.</summary>
    Task<ConfigurationEntry?> GetByKeyAndScopeAsync(
        string key,
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken);

    /// <summary>Returns all entries for the given scope and optional scope reference.</summary>
    Task<IReadOnlyList<ConfigurationEntry>> GetAllByScopeAsync(
        ConfigurationScope scope,
        string? scopeReferenceId,
        CancellationToken cancellationToken);

    /// <summary>Returns all entries for the given configuration key across all scopes.</summary>
    Task<IReadOnlyList<ConfigurationEntry>> GetAllByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>Persists a new configuration entry.</summary>
    Task AddAsync(ConfigurationEntry entry, CancellationToken cancellationToken);

    /// <summary>Persists changes to an existing configuration entry.</summary>
    Task UpdateAsync(ConfigurationEntry entry, CancellationToken cancellationToken);

    /// <summary>Removes a configuration entry from persistence.</summary>
    Task DeleteAsync(ConfigurationEntry entry, CancellationToken cancellationToken);
}
