using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ActivateUserFeature = NexTraceOne.IdentityAccess.Application.Features.ActivateUser.ActivateUser;
using AssignRoleFeature = NexTraceOne.IdentityAccess.Application.Features.AssignRole.AssignRole;
using CreateUserFeature = NexTraceOne.IdentityAccess.Application.Features.CreateUser.CreateUser;
using DeactivateUserFeature = NexTraceOne.IdentityAccess.Application.Features.DeactivateUser.DeactivateUser;
using GetPersonaConfigFeature = NexTraceOne.IdentityAccess.Application.Features.GetPersonaConfig.GetPersonaConfig;
using GetUserProfileFeature = NexTraceOne.IdentityAccess.Application.Features.GetUserProfile.GetUserProfile;
using ListActiveSessionsFeature = NexTraceOne.IdentityAccess.Application.Features.ListActiveSessions.ListActiveSessions;
using ListTenantUsersFeature = NexTraceOne.IdentityAccess.Application.Features.ListTenantUsers.ListTenantUsers;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

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
            return result.ToCreatedResult(r => $"/api/v1/identity/users/{r}", localizer);
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
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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
            ICurrentTenant currentTenant,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateUserFeature.Command(userId, currentTenant.Id), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");

        group.MapPut("/users/{userId:guid}/activate", async (
            Guid userId,
            ICurrentTenant currentTenant,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ActivateUserFeature.Command(userId, currentTenant.Id), cancellationToken);
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

        // ── Wave X.3 — Persona-Aware Adaptive Navigation ───────────────────────────────────────────
        group.MapGet("/me/persona-config", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPersonaConfigFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization()
          .WithName("GetPersonaConfig");
    }

    /// <summary>
    /// DTO para atribuição de role a um utilizador num determinado tenant.
    /// </summary>
    internal sealed record AssignRoleRequest(Guid TenantId, Guid RoleId);
}
