using Microsoft.AspNetCore.Builder;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ActivateUserFeature = NexTraceOne.Identity.Application.Features.ActivateUser.ActivateUser;
using AssignRoleFeature = NexTraceOne.Identity.Application.Features.AssignRole.AssignRole;
using CreateUserFeature = NexTraceOne.Identity.Application.Features.CreateUser.CreateUser;
using DeactivateUserFeature = NexTraceOne.Identity.Application.Features.DeactivateUser.DeactivateUser;
using GetUserProfileFeature = NexTraceOne.Identity.Application.Features.GetUserProfile.GetUserProfile;
using ListActiveSessionsFeature = NexTraceOne.Identity.Application.Features.ListActiveSessions.ListActiveSessions;
using ListTenantUsersFeature = NexTraceOne.Identity.Application.Features.ListTenantUsers.ListTenantUsers;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Endpoints de gestão de utilizadores do módulo Identity.
/// Inclui criação, consulta de perfil, listagem por tenant, atribuição de roles,
/// ativação/desativação e listagem de sessões ativas.
/// Todos os endpoints exigem permissões administrativas específicas.
/// </summary>
internal static class UserEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de gestão de utilizadores no grupo raiz do módulo Identity.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/users", async (
            CreateUserFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/identity/users/{0}", localizer);
        }).RequirePermission("identity:users:write");

        group.MapGet("/users/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetUserProfileFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        group.MapGet("/tenants/{tenantId:guid}/users", async (
            Guid tenantId,
            string? search,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListTenantUsersFeature.Query(tenantId, search, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        group.MapPost("/users/{userId:guid}/roles", async (
            Guid userId,
            AssignRoleRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new AssignRoleFeature.Command(userId, request.TenantId, request.RoleId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:roles:assign");

        group.MapPut("/users/{userId:guid}/deactivate", async (
            Guid userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateUserFeature.Command(userId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");

        group.MapPut("/users/{userId:guid}/activate", async (
            Guid userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ActivateUserFeature.Command(userId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");

        group.MapGet("/users/{userId:guid}/sessions", async (
            Guid userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListActiveSessionsFeature.Query(userId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:read");
    }

    /// <summary>
    /// DTO para atribuição de role a um utilizador num determinado tenant.
    /// </summary>
    internal sealed record AssignRoleRequest(Guid TenantId, Guid RoleId);
}
