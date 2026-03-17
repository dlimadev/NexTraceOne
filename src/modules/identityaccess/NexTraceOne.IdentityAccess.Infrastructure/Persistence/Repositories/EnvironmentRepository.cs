using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

using Environment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de ambientes e acessos por ambiente.
/// Consultas filtram por tenant e status ativo para respeitar isolamento multi-tenant.
/// </summary>
internal sealed class EnvironmentRepository(IdentityDbContext dbContext) : IEnvironmentRepository
{
    /// <inheritdoc />
    public async Task<Environment?> GetByIdAsync(EnvironmentId id, CancellationToken cancellationToken)
        => await dbContext.Environments
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Environment>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.Environments
            .Where(e => e.TenantId == tenantId && e.IsActive)
            .OrderBy(e => e.SortOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> SlugExistsAsync(TenantId tenantId, string slug, CancellationToken cancellationToken)
        => await dbContext.Environments
            .AnyAsync(e => e.TenantId == tenantId && e.Slug == slug, cancellationToken);

    /// <inheritdoc />
    public void Add(Environment environment)
        => dbContext.Environments.Add(environment);

    /// <inheritdoc />
    public async Task<EnvironmentAccess?> GetAccessAsync(
        UserId userId,
        TenantId tenantId,
        EnvironmentId environmentId,
        CancellationToken cancellationToken)
        => await dbContext.EnvironmentAccesses
            .FirstOrDefaultAsync(a =>
                a.UserId == userId
                && a.TenantId == tenantId
                && a.EnvironmentId == environmentId
                && a.IsActive,
                cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnvironmentAccess>> ListUserAccessesAsync(
        UserId userId,
        TenantId tenantId,
        CancellationToken cancellationToken)
        => await dbContext.EnvironmentAccesses
            .Where(a => a.UserId == userId && a.TenantId == tenantId && a.IsActive)
            .OrderBy(a => a.GrantedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnvironmentAccess>> ListExpiredAccessesAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await dbContext.EnvironmentAccesses
            .Where(a => a.IsActive && a.ExpiresAt != null && a.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void AddAccess(EnvironmentAccess access)
        => dbContext.EnvironmentAccesses.Add(access);
}
