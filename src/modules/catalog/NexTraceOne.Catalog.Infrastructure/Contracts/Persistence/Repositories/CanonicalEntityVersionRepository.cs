using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de versões de entidades canónicas para histórico e diff.
/// </summary>
internal sealed class CanonicalEntityVersionRepository(ContractsDbContext context)
    : ICanonicalEntityVersionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CanonicalEntityVersion>> ListByEntityIdAsync(
        CanonicalEntityId entityId,
        CancellationToken cancellationToken = default)
        => await context.CanonicalEntityVersions
            .Where(v => v.CanonicalEntityId == entityId)
            .OrderByDescending(v => v.PublishedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<CanonicalEntityVersion?> GetByVersionAsync(
        CanonicalEntityId entityId,
        string version,
        CancellationToken cancellationToken = default)
        => await context.CanonicalEntityVersions
            .SingleOrDefaultAsync(
                v => v.CanonicalEntityId == entityId && v.Version == version,
                cancellationToken);

    /// <inheritdoc />
    public void Add(CanonicalEntityVersion version) => context.CanonicalEntityVersions.Add(version);
}
