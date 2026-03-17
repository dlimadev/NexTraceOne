using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de Tenants.
/// </summary>
internal sealed class TenantRepository(IdentityDbContext context) : ITenantRepository
{
    /// <inheritdoc />
    public async Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken)
        => await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => await context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<TenantId, Tenant>> GetByIdsAsync(
        IReadOnlyCollection<TenantId> ids,
        CancellationToken cancellationToken)
    {
        var tenants = await context.Tenants
            .Where(t => ids.Contains(t.Id))
            .ToListAsync(cancellationToken);

        return tenants.ToDictionary(t => t.Id, t => t);
    }

    /// <inheritdoc />
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        => await context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);

    /// <inheritdoc />
    public void Add(Tenant tenant)
        => context.Tenants.Add(tenant);
}
