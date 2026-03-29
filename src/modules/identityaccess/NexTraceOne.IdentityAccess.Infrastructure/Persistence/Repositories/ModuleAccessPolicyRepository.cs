using Microsoft.EntityFrameworkCore;

using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de políticas de acesso granular por módulo/página/ação via EF Core.
/// Resolve políticas com prioridade: tenant-specific > sistema (TenantId nulo).
/// </summary>
internal sealed class ModuleAccessPolicyRepository(IdentityDbContext context) : IModuleAccessPolicyRepository
{
    /// <inheritdoc />
    public async Task<bool?> IsAllowedAsync(
        RoleId roleId,
        TenantId? tenantId,
        string module,
        string page,
        string action,
        CancellationToken cancellationToken)
    {
        // Prioridade 1: política específica do tenant
        if (tenantId is not null)
        {
            var tenantPolicy = await context.ModuleAccessPolicies
                .Where(p => p.RoleId == roleId
                            && p.TenantId == tenantId
                            && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            var tenantMatch = tenantPolicy.FirstOrDefault(p => p.Matches(module, page, action));
            if (tenantMatch is not null)
                return tenantMatch.IsAllowed;
        }

        // Prioridade 2: política do sistema (TenantId nulo)
        var systemPolicies = await context.ModuleAccessPolicies
            .Where(p => p.RoleId == roleId
                        && p.TenantId == null
                        && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var systemMatch = systemPolicies.FirstOrDefault(p => p.Matches(module, page, action));
        return systemMatch?.IsAllowed;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModuleAccessPolicy>> GetPoliciesForRoleAsync(
        RoleId roleId,
        TenantId? tenantId,
        string module,
        CancellationToken cancellationToken)
    {
        return await context.ModuleAccessPolicies
            .Where(p => p.RoleId == roleId
                        && (p.TenantId == tenantId || p.TenantId == null)
                        && p.Module == module
                        && p.IsActive)
            .OrderBy(p => p.Page)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ModuleAccessPolicy policy, CancellationToken cancellationToken)
    {
        await context.ModuleAccessPolicies.AddAsync(policy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<ModuleAccessPolicy> policies, CancellationToken cancellationToken)
    {
        await context.ModuleAccessPolicies.AddRangeAsync(policies, cancellationToken);
    }
}
