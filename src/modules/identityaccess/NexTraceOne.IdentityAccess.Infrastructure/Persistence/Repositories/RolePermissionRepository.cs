using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de mapeamentos papel→permissão persistidos via EF Core.
/// Resolve permissões com prioridade: tenant-specific > sistema (TenantId nulo).
/// </summary>
internal sealed class RolePermissionRepository(
    IdentityDbContext context,
    ILogger<RolePermissionRepository> logger) : IRolePermissionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPermissionCodesForRoleAsync(
        RoleId roleId,
        TenantId? tenantId,
        CancellationToken cancellationToken)
    {
        // Prioridade: se existem mapeamentos para o tenant, usar esses;
        // caso contrário, usar mapeamentos do sistema (TenantId nulo).
        try
        {
            var tenantMappings = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId
                             && rp.TenantId != null
                             && rp.TenantId == tenantId
                             && rp.IsActive)
                .Select(rp => rp.PermissionCode)
                .ToListAsync(cancellationToken);

            if (tenantMappings.Count > 0)
                return tenantMappings;

            // Fallback: mapeamentos do sistema (TenantId nulo).
            return await context.RolePermissions
                .Where(rp => rp.RoleId == roleId
                             && rp.TenantId == null
                             && rp.IsActive)
                .Select(rp => rp.PermissionCode)
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Table does not exist yet (migrations not applied). Return empty result
            // to allow the application to continue in development scenarios.
            logger.LogWarning(ex,
                "Bootstrap: iam_role_permissions table missing (42P01) for role {RoleId}. Migration may not have been applied.",
                roleId.Value);
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasMappingsForRoleAsync(
        RoleId roleId,
        TenantId? tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId
                                && (rp.TenantId == tenantId || rp.TenantId == null)
                                && rp.IsActive,
                    cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Table missing — treat as no mappings found.
            logger.LogWarning(ex,
                "Bootstrap: iam_role_permissions table missing (42P01) for HasMappingsForRoleAsync role {RoleId}. Migration may not have been applied.",
                roleId.Value);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken)
    {
        await context.RolePermissions.AddAsync(rolePermission, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken)
    {
        await context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
    }
}
