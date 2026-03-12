using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using ActivateUserFeature = NexTraceOne.Identity.Application.Features.ActivateUser.ActivateUser;
using AssignRoleFeature = NexTraceOne.Identity.Application.Features.AssignRole.AssignRole;
using ChangePasswordFeature = NexTraceOne.Identity.Application.Features.ChangePassword.ChangePassword;
using CreateUserFeature = NexTraceOne.Identity.Application.Features.CreateUser.CreateUser;
using DeactivateUserFeature = NexTraceOne.Identity.Application.Features.DeactivateUser.DeactivateUser;
using FederatedLoginFeature = NexTraceOne.Identity.Application.Features.FederatedLogin.FederatedLogin;
using GetCurrentUserFeature = NexTraceOne.Identity.Application.Features.GetCurrentUser.GetCurrentUser;
using GetUserProfileFeature = NexTraceOne.Identity.Application.Features.GetUserProfile.GetUserProfile;
using ListActiveSessionsFeature = NexTraceOne.Identity.Application.Features.ListActiveSessions.ListActiveSessions;
using ListPermissionsFeature = NexTraceOne.Identity.Application.Features.ListPermissions.ListPermissions;
using ListRolesFeature = NexTraceOne.Identity.Application.Features.ListRoles.ListRoles;
using ListTenantUsersFeature = NexTraceOne.Identity.Application.Features.ListTenantUsers.ListTenantUsers;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using LogoutFeature = NexTraceOne.Identity.Application.Features.Logout.Logout;
using RefreshTokenFeature = NexTraceOne.Identity.Application.Features.RefreshToken.RefreshToken;
using RevokeSessionFeature = NexTraceOne.Identity.Application.Features.RevokeSession.RevokeSession;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Identity.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Organizado em: auth (público), users (admin), roles/permissions (consulta).
/// </summary>
public sealed class IdentityEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity");

        MapAuthEndpoints(group);
        MapUserEndpoints(group);
        MapRoleAndPermissionEndpoints(group);
    }

    /// <summary>Endpoints de autenticação — login, logout, refresh, revoke, /me.</summary>
    private static void MapAuthEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var authGroup = group.MapGroup("/auth");

        authGroup.MapPost("/login", async (
            LocalLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous();

        authGroup.MapPost("/federated", async (
            FederatedLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous();

        authGroup.MapPost("/refresh", async (
            RefreshTokenFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous();

        authGroup.MapPost("/logout", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LogoutFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        authGroup.MapPost("/revoke", async (
            RevokeSessionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        authGroup.MapGet("/me", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetCurrentUserFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        authGroup.MapPut("/password", async (
            ChangePasswordFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();
    }

    /// <summary>Endpoints de gestão de usuários — CRUD, ativar/desativar, sessões, roles.</summary>
    private static void MapUserEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/users", async (
            CreateUserFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/identity/users/{0}", localizer);
        }).RequireAuthorization();

        group.MapGet("/users/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetUserProfileFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

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
        }).RequireAuthorization();

        group.MapPost("/users/{userId:guid}/roles", async (
            Guid userId,
            AssignRoleRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new AssignRoleFeature.Command(userId, request.TenantId, request.RoleId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        group.MapPut("/users/{userId:guid}/deactivate", async (
            Guid userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateUserFeature.Command(userId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        group.MapPut("/users/{userId:guid}/activate", async (
            Guid userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ActivateUserFeature.Command(userId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        group.MapGet("/users/{userId:guid}/sessions", async (
            Guid userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListActiveSessionsFeature.Query(userId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();
    }

    /// <summary>Endpoints de consulta de papéis e permissões do sistema.</summary>
    private static void MapRoleAndPermissionEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapGet("/roles", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListRolesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        group.MapGet("/permissions", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListPermissionsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();
    }

    private sealed record AssignRoleRequest(Guid TenantId, Guid RoleId);
}
