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

    /// <summary>
    /// Returns the most recent audit entries whose key starts with the given prefix, limited by count.
    /// Useful for retrieving the audit trail for an entire module namespace (e.g. "change.release.").
    /// </summary>
    Task<IReadOnlyList<ConfigurationAuditEntry>> GetByKeyPrefixAsync(string keyPrefix, int limit, CancellationToken cancellationToken);
}
