using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    {
        try
        {
            return await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            // Column missing in database schema (migration not applied). In development
            // scenarios return null to allow the application to continue and let
            // migrations/seeding complete. Do not swallow other database errors.
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            return await context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<TenantId, Tenant>> GetByIdsAsync(
        IReadOnlyCollection<TenantId> ids,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenants = await context.Tenants
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken);

            return tenants.ToDictionary(t => t.Id, t => t);
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            // Schema missing columns expected by the entity mapping. Return an
            // empty dictionary to avoid crashing development flows while
            // migrations are applied.
            return new Dictionary<TenantId, Tenant>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            return await context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42703")
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Add(Tenant tenant)
        => context.Tenants.Add(tenant);
}
