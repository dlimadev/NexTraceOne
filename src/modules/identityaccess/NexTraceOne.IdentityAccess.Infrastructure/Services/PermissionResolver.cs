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
}
