using Microsoft.AspNetCore.Builder;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using AssignRoleFeature = NexTraceOne.Identity.Application.Features.AssignRole.AssignRole;
using CreateUserFeature = NexTraceOne.Identity.Application.Features.CreateUser.CreateUser;
using FederatedLoginFeature = NexTraceOne.Identity.Application.Features.FederatedLogin.FederatedLogin;
using GetUserProfileFeature = NexTraceOne.Identity.Application.Features.GetUserProfile.GetUserProfile;
using ListTenantUsersFeature = NexTraceOne.Identity.Application.Features.ListTenantUsers.ListTenantUsers;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using RefreshTokenFeature = NexTraceOne.Identity.Application.Features.RefreshToken.RefreshToken;
using RevokeSessionFeature = NexTraceOne.Identity.Application.Features.RevokeSession.RevokeSession;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Identity.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class IdentityEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity");
        var authGroup = group.MapGroup("/auth");

        authGroup.MapPost("/login", async (
            LocalLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        authGroup.MapPost("/federated", async (
            FederatedLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        authGroup.MapPost("/refresh", async (
            RefreshTokenFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        authGroup.MapPost("/revoke", async (
            RevokeSessionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/users", async (
            CreateUserFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/identity/users/{0}", localizer);
        });

        group.MapGet("/users/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetUserProfileFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/tenants/{tenantId:guid}/users", async (
            Guid tenantId,
            string? search,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListTenantUsersFeature.Query(tenantId, search, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/users/{userId:guid}/roles", async (
            Guid userId,
            AssignRoleRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new AssignRoleFeature.Command(userId, request.TenantId, request.RoleId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }

    private sealed record AssignRoleRequest(Guid TenantId, Guid RoleId);
}
