using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Repository abstraction for ConfigurationAuditEntry.
/// Provides write-append and read operations for the configuration audit trail.
/// </summary>
public interface IConfigurationAuditRepository
{
    /// <summary>Persists a new audit entry for a configuration change.</summary>
    Task AddAsync(ConfigurationAuditEntry auditEntry, CancellationToken cancellationToken);

    /// <summary>Returns the most recent audit entries for a given configuration key, limited by count.</summary>
    Task<IReadOnlyList<ConfigurationAuditEntry>> GetByKeyAsync(string key, int limit, CancellationToken cancellationToken);
}
