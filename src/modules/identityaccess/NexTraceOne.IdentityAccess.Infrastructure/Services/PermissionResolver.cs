using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IPermissionResolver"/> com estratégia DB-first e fallback estático.
///
/// Lógica de resolução:
/// 1. Verifica se existem mapeamentos persistidos em base de dados para o par role+tenant.
/// 2. Se existirem, retorna as permissões da base de dados (suportando personalização por tenant).
/// 3. Se não existirem, recorre ao <see cref="RolePermissionCatalog"/> como fallback
///    (compatibilidade retroativa para instalações sem seed executado).
///
/// Suporta multi-role: resolve permissões para múltiplos papéis e retorna a UNIÃO.
/// </summary>
internal sealed class PermissionResolver(
    IRolePermissionRepository rolePermissionRepository) : IPermissionResolver
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        RoleId roleId,
        string roleName,
        TenantId? tenantId,
        CancellationToken cancellationToken)
    {
        var hasMappings = await rolePermissionRepository.HasMappingsForRoleAsync(
            roleId, tenantId, cancellationToken);

        if (hasMappings)
        {
            return await rolePermissionRepository.GetPermissionCodesForRoleAsync(
                roleId, tenantId, cancellationToken);
        }

        // Fallback: catálogo estático para compatibilidade retroativa.
        return RolePermissionCatalog.GetPermissionsForRole(roleName);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ResolvePermissionsForMultipleRolesAsync(
        IReadOnlyList<(RoleId RoleId, string RoleName)> roleAssignments,
        TenantId? tenantId,
        CancellationToken cancellationToken)
    {
        if (roleAssignments.Count == 0)
            return Array.Empty<string>();

        // Caso com um único papel — evita overhead de HashSet.
        if (roleAssignments.Count == 1)
        {
            return await ResolvePermissionsAsync(
                roleAssignments[0].RoleId,
                roleAssignments[0].RoleName,
                tenantId,
                cancellationToken);
        }

        // Múltiplos papéis — UNIÃO de permissões sem duplicatas.
        var allPermissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (roleId, roleName) in roleAssignments)
        {
            var permissions = await ResolvePermissionsAsync(
                roleId, roleName, tenantId, cancellationToken);

            foreach (var permission in permissions)
            {
                allPermissions.Add(permission);
            }
        }

        return allPermissions.Order(StringComparer.Ordinal).ToList().AsReadOnly();
    }
}
