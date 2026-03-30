using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Provider que resolve autorização via políticas de acesso ao nível de módulo/página/ação,
/// consultando a tabela iam_module_access_policies.
///
/// Estratégia de resolução:
/// 1. Traduz o permission code plano ("ai:runtime:write") para o triplo ("AI", "Runtime", "Write")
///    usando <see cref="PermissionCodeMapper"/>.
/// 2. Consulta <see cref="IModuleAccessPolicyRepository.IsAllowedAsync"/> com prioridade tenant > sistema.
/// 3. Se nenhuma política se aplica na base de dados, consulta o catálogo estático
///    <see cref="ModuleAccessPolicyCatalog"/> como fallback — garantindo compatibilidade retroativa
///    para instalações sem seed executado.
///
/// Retorna:
/// - <c>true</c> se uma política concede acesso.
/// - <c>false</c> se uma política nega acesso explicitamente (deny override).
/// - <c>null</c> se nenhuma política se aplica (o handler continua a cascata).
/// </summary>
internal sealed class ModuleAccessPermissionProvider(
    IModuleAccessPolicyRepository moduleAccessPolicyRepository,
    IRoleRepository roleRepository) : IModuleAccessPermissionProvider
{
    /// <inheritdoc />
    public async Task<bool?> HasModuleAccessAsync(
        string userId,
        string roleId,
        string tenantId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var mapped = PermissionCodeMapper.Map(permissionCode);
        if (mapped is null)
            return null;

        return await ResolveAccessAsync(roleId, tenantId, mapped.Module, mapped.Page, mapped.Action, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool?> HasModuleAccessDirectAsync(
        string userId,
        string roleId,
        string tenantId,
        string module,
        string page,
        string action,
        CancellationToken cancellationToken)
    {
        return await ResolveAccessAsync(roleId, tenantId, module, page, action, cancellationToken);
    }

    private async Task<bool?> ResolveAccessAsync(
        string roleId,
        string tenantId,
        string module,
        string page,
        string action,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(roleId, out var roleGuid))
            return null;

        var parsedRoleId = RoleId.From(roleGuid);
        TenantId? parsedTenantId = Guid.TryParse(tenantId, out var tenantGuid)
            ? TenantId.From(tenantGuid)
            : null;

        // 1. Consultar políticas persistidas na base de dados (tenant > sistema)
        var dbResult = await moduleAccessPolicyRepository.IsAllowedAsync(
            parsedRoleId,
            parsedTenantId,
            module,
            page,
            action,
            cancellationToken);

        if (dbResult.HasValue)
            return dbResult.Value;

        // 2. Fallback: catálogo estático para compatibilidade retroativa
        var role = await roleRepository.GetByIdAsync(parsedRoleId, cancellationToken);
        if (role is null)
            return null;

        var catalogPolicies = ModuleAccessPolicyCatalog.GetPoliciesForRole(role.Name);
        if (catalogPolicies.Count == 0)
            return null;

        var catalogMatch = catalogPolicies.FirstOrDefault(p =>
            string.Equals(p.Module, module, StringComparison.OrdinalIgnoreCase) &&
            (p.Page == "*" || string.Equals(p.Page, page, StringComparison.OrdinalIgnoreCase)) &&
            (p.Action == "*" || string.Equals(p.Action, action, StringComparison.OrdinalIgnoreCase)));

        return catalogMatch?.IsAllowed;
    }
}
