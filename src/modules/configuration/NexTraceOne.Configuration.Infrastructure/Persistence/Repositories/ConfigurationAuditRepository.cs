using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ConfigurationAuditEntry usando EF Core.
/// </summary>
internal sealed class ConfigurationAuditRepository(ConfigurationDbContext context)
    : IConfigurationAuditRepository
{
    public async Task AddAsync(ConfigurationAuditEntry auditEntry, CancellationToken cancellationToken)
        => await context.AuditEntries.AddAsync(auditEntry, cancellationToken);

    public async Task<IReadOnlyList<ConfigurationAuditEntry>> GetByKeyAsync(
        string key,
        int limit,
        CancellationToken cancellationToken)
        => await context.AuditEntries
            .Where(a => a.Key == key)
            .OrderByDescending(a => a.ChangedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConfigurationAuditEntry>> GetByKeyPrefixAsync(
        string keyPrefix,
        int limit,
        CancellationToken cancellationToken)
        => await context.AuditEntries
            .Where(a => a.Key.StartsWith(keyPrefix))
            .OrderByDescending(a => a.ChangedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
