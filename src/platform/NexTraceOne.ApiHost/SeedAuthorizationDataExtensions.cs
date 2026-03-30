using MediatR;

using SeedRolePermissions = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultRolePermissions.SeedDefaultRolePermissions;
using SeedModuleAccessPolicies = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultModuleAccessPolicies.SeedDefaultModuleAccessPolicies;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Extension method que executa o seed dos dados de autorização obrigatórios:
/// permissões por papel (iam_role_permissions) e políticas de acesso por módulo/página/ação
/// (iam_module_access_policies) para todos os papéis do sistema.
///
/// Executa em TODOS os ambientes (produção, staging, desenvolvimento) porque:
/// - O pipeline de autorização depende destes dados para os passos 2 (DB RolePermission)
///   e 3 (DB ModuleAccessPolicy) da cascata.
/// - Sem estes dados, a autorização recorre apenas ao catálogo estático (fallback),
///   perdendo a capacidade de personalização por tenant sem redeploy.
///
/// O seed é idempotente: apenas cria registos para papéis que ainda não possuem
/// políticas/permissões no banco (sistema, TenantId nulo). Seguro de re-executar.
///
/// Fonte de verdade: <see cref="NexTraceOne.IdentityAccess.Domain.Entities.RolePermissionCatalog"/>
/// e <see cref="NexTraceOne.IdentityAccess.Domain.Entities.ModuleAccessPolicyCatalog"/>.
/// </summary>
public static class SeedAuthorizationDataExtensions
{
    /// <summary>
    /// Semeia os dados de autorização obrigatórios para todos os papéis do sistema.
    /// Requer que as migrations EF Core já tenham sido aplicadas (tabelas iam_roles,
    /// iam_role_permissions e iam_module_access_policies existam).
    /// </summary>
    public static async Task SeedAuthorizationDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // 1. Seed de permissões planas por papel (iam_role_permissions)
        await SeedRolePermissionsAsync(sender, logger);

        // 2. Seed de políticas de acesso módulo/página/ação (iam_module_access_policies)
        await SeedModuleAccessPoliciesAsync(sender, logger);
    }

    private static async Task SeedRolePermissionsAsync(ISender sender, ILogger logger)
    {
        try
        {
            var result = await sender.Send(new SeedRolePermissions.Command());

            if (result.IsSuccess && result.Value.RolesSeeded > 0)
            {
                logger.LogInformation(
                    "Authorization seed: role permissions seeded. " +
                    "Roles: {RolesSeeded}, Permissions: {PermissionsCreated}.",
                    result.Value.RolesSeeded,
                    result.Value.TotalPermissionsCreated);
            }
            else if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Authorization seed: role permissions already up-to-date (no changes).");
            }
            else
            {
                logger.LogWarning(
                    "Authorization seed: role permissions seeding returned failure. " +
                    "The application will continue using the static catalog as fallback.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Authorization seed: role permissions seeding failed. " +
                "This may be expected if the Identity database schema has not been created yet. " +
                "The application will continue using the static catalog as fallback.");
        }
    }

    private static async Task SeedModuleAccessPoliciesAsync(ISender sender, ILogger logger)
    {
        try
        {
            var result = await sender.Send(new SeedModuleAccessPolicies.Command());

            if (result.IsSuccess && result.Value.RolesSeeded > 0)
            {
                logger.LogInformation(
                    "Authorization seed: module access policies seeded. " +
                    "Roles: {RolesSeeded}, Policies: {PoliciesCreated}.",
                    result.Value.RolesSeeded,
                    result.Value.TotalPoliciesCreated);
            }
            else if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Authorization seed: module access policies already up-to-date (no changes).");
            }
            else
            {
                logger.LogWarning(
                    "Authorization seed: module access policies seeding returned failure. " +
                    "The application will continue using the static catalog as fallback.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Authorization seed: module access policies seeding failed. " +
                "This may be expected if the Identity database schema has not been created yet. " +
                "The application will continue using the static catalog as fallback.");
        }
    }
}
