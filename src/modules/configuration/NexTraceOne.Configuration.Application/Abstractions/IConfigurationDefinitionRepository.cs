using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Repository abstraction for ConfigurationDefinition aggregate.
/// Provides read and write operations for configuration metadata definitions.
/// </summary>
public interface IConfigurationDefinitionRepository
{
    /// <summary>Returns the definition matching the given unique key, or null if not found.</summary>
    Task<ConfigurationDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>Returns all registered configuration definitions.</summary>
    Task<IReadOnlyList<ConfigurationDefinition>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Persists a new configuration definition.</summary>
    Task AddAsync(ConfigurationDefinition definition, CancellationToken cancellationToken);

    /// <summary>Persists changes to an existing configuration definition.</summary>
    Task UpdateAsync(ConfigurationDefinition definition, CancellationToken cancellationToken);
}
