using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ListRolesFeature = NexTraceOne.IdentityAccess.Application.Features.ListRoles.ListRoles;
using ListPermissionsFeature = NexTraceOne.IdentityAccess.Application.Features.ListPermissions.ListPermissions;
using SeedDefaultsFeature = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultRolePermissions.SeedDefaultRolePermissions;
using SeedModulePoliciesFeature = NexTraceOne.IdentityAccess.Application.Features.SeedDefaultModuleAccessPolicies.SeedDefaultModuleAccessPolicies;
using CreateRoleFeature = NexTraceOne.IdentityAccess.Application.Features.CreateRole.CreateRole;
using UpdateRoleFeature = NexTraceOne.IdentityAccess.Application.Features.UpdateRole.UpdateRole;
using DeleteRoleFeature = NexTraceOne.IdentityAccess.Application.Features.DeleteRole.DeleteRole;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de consulta de papéis (roles) e permissões do sistema.
/// Permitem listar os roles disponíveis e as permissões definidas,
/// útil para interfaces de administração e atribuição de acessos.
/// Inclui endpoints de seed para inicialização dos mapeamentos padrão.
/// </summary>
internal static class RolePermissionEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de consulta de roles e permissões no grupo raiz do módulo Identity.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapGet("/roles", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListRolesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:roles:read");

        group.MapGet("/permissions", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListPermissionsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:permissions:read");

        group.MapPost("/role-permissions/seed-defaults", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SeedDefaultsFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        group.MapPost("/module-access-policies/seed-defaults", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SeedModulePoliciesFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        // ── CRUD Roles ───────────────────────────────────────────

        group.MapPost("/roles", async (
            CreateRoleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:roles:write");

        group.MapPut("/roles/{roleId:guid}", async (
            Guid roleId,
            UpdateRoleRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateRoleFeature.Command(roleId, body.Name, body.Description), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:roles:write");

        group.MapDelete("/roles/{roleId:guid}", async (
            Guid roleId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteRoleFeature.Command(roleId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:roles:write");
    }

    /// <summary>Corpo do pedido de atualização de role (sem o Id que vem na URL).</summary>
    internal sealed record UpdateRoleRequest(string Name, string Description);
}
