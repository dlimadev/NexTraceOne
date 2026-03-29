using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Provider que resolve permissões a partir dos mapeamentos papel→permissão
/// persistidos em base de dados. Implementa <see cref="IDatabasePermissionProvider"/>
/// para integração com o PermissionAuthorizationHandler.
///
/// Estratégia de resolução:
/// 1. Consulta mapeamentos na tabela iam_role_permissions para o papel e tenant.
/// 2. Se existem mapeamentos persistidos, usa esses (prioridade: tenant > sistema).
/// 3. Se não existem mapeamentos, retorna false (fallback para JWT claims no handler).
///
/// Esta implementação permite que um PlatformAdmin configure permissões customizadas
/// por tenant diretamente na UI, sem necessidade de redeploy ou alteração de código.
/// </summary>
internal sealed class DatabasePermissionProvider(
    IRolePermissionRepository rolePermissionRepository) : IDatabasePermissionProvider
{
    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        string userId,
        string roleId,
        string tenantId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(roleId, out var roleGuid))
            return false;

        TenantId? parsedTenantId = Guid.TryParse(tenantId, out var tenantGuid)
            ? TenantId.From(tenantGuid)
            : null;

        var roleParsed = RoleId.From(roleGuid);

        // Verificar se existem mapeamentos persistidos para este papel
        var hasMappings = await rolePermissionRepository.HasMappingsForRoleAsync(
            roleParsed, parsedTenantId, cancellationToken);

        if (!hasMappings)
            return false;

        // Resolver permissões do banco (tenant-specific > sistema)
        var permissions = await rolePermissionRepository.GetPermissionCodesForRoleAsync(
            roleParsed, parsedTenantId, cancellationToken);

        return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }
}
