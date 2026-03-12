using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ActivateUserFeature = NexTraceOne.Identity.Application.Features.ActivateUser.ActivateUser;
using AssignRoleFeature = NexTraceOne.Identity.Application.Features.AssignRole.AssignRole;
using ChangePasswordFeature = NexTraceOne.Identity.Application.Features.ChangePassword.ChangePassword;
using CreateDelegationFeature = NexTraceOne.Identity.Application.Features.CreateDelegation.CreateDelegation;
using CreateUserFeature = NexTraceOne.Identity.Application.Features.CreateUser.CreateUser;
using DeactivateUserFeature = NexTraceOne.Identity.Application.Features.DeactivateUser.DeactivateUser;
using DecideJitAccessFeature = NexTraceOne.Identity.Application.Features.DecideJitAccess.DecideJitAccess;
using FederatedLoginFeature = NexTraceOne.Identity.Application.Features.FederatedLogin.FederatedLogin;
using GetCurrentUserFeature = NexTraceOne.Identity.Application.Features.GetCurrentUser.GetCurrentUser;
using GetUserProfileFeature = NexTraceOne.Identity.Application.Features.GetUserProfile.GetUserProfile;
using ListActiveSessionsFeature = NexTraceOne.Identity.Application.Features.ListActiveSessions.ListActiveSessions;
using ListBreakGlassFeature = NexTraceOne.Identity.Application.Features.ListBreakGlassRequests.ListBreakGlassRequests;
using ListDelegationsFeature = NexTraceOne.Identity.Application.Features.ListDelegations.ListDelegations;
using ListJitAccessFeature = NexTraceOne.Identity.Application.Features.ListJitAccessRequests.ListJitAccessRequests;
using ListPermissionsFeature = NexTraceOne.Identity.Application.Features.ListPermissions.ListPermissions;
using ListRolesFeature = NexTraceOne.Identity.Application.Features.ListRoles.ListRoles;
using ListTenantUsersFeature = NexTraceOne.Identity.Application.Features.ListTenantUsers.ListTenantUsers;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using LogoutFeature = NexTraceOne.Identity.Application.Features.Logout.Logout;
using RefreshTokenFeature = NexTraceOne.Identity.Application.Features.RefreshToken.RefreshToken;
using RequestBreakGlassFeature = NexTraceOne.Identity.Application.Features.RequestBreakGlass.RequestBreakGlass;
using RequestJitAccessFeature = NexTraceOne.Identity.Application.Features.RequestJitAccess.RequestJitAccess;
using RevokeDelegationFeature = NexTraceOne.Identity.Application.Features.RevokeDelegation.RevokeDelegation;
using RevokeBreakGlassFeature = NexTraceOne.Identity.Application.Features.RevokeBreakGlass.RevokeBreakGlass;
using RevokeSessionFeature = NexTraceOne.Identity.Application.Features.RevokeSession.RevokeSession;
using ListMyTenantsFeature = NexTraceOne.Identity.Application.Features.ListMyTenants.ListMyTenants;
using SelectTenantFeature = NexTraceOne.Identity.Application.Features.SelectTenant.SelectTenant;
using StartOidcLoginFeature = NexTraceOne.Identity.Application.Features.StartOidcLogin.StartOidcLogin;
using OidcCallbackFeature = NexTraceOne.Identity.Application.Features.OidcCallback.OidcCallback;
using StartAccessReviewFeature = NexTraceOne.Identity.Application.Features.StartAccessReviewCampaign.StartAccessReviewCampaign;
using ListAccessReviewFeature = NexTraceOne.Identity.Application.Features.ListAccessReviewCampaigns.ListAccessReviewCampaigns;
using GetAccessReviewFeature = NexTraceOne.Identity.Application.Features.GetAccessReviewCampaign.GetAccessReviewCampaign;
using DecideAccessReviewItemFeature = NexTraceOne.Identity.Application.Features.DecideAccessReviewItem.DecideAccessReviewItem;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Identity.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Organizado em: auth (público), users (admin), roles/permissions (consulta),
/// break-glass, jit-access, delegations, tenants, access-review.
/// </summary>
public sealed class IdentityEndpointModule
{
    /// <summary>
    /// Registra endpoints no roteador do ASP.NET Core.
    /// Cada endpoint possui autorização granular baseada em permissão.
    /// Endpoints públicos (login, federated, refresh, oidc) usam AllowAnonymous.
    /// Endpoints de self-service (logout, /me, password) exigem apenas autenticação.
    /// Endpoints administrativos exigem permissão específica via RequirePermission.
    /// </summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity");

        MapAuthEndpoints(group);
        MapUserEndpoints(group);
        MapRoleAndPermissionEndpoints(group);
        MapBreakGlassEndpoints(group);
        MapJitAccessEndpoints(group);
        MapDelegationEndpoints(group);
        MapTenantEndpoints(group);
        MapAccessReviewEndpoints(group);
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

        // ── OIDC Redirect Flow ──────────────────────────────────────────────────
        // POST /auth/oidc/start — inicia o fluxo OIDC e retorna a URL de redirect
        authGroup.MapPost("/oidc/start", async (
            StartOidcLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous();

        // GET /auth/oidc/callback — recebe o callback do provider OIDC
        // Aceita tanto GET (redirect do browser) quanto POST (para testes)
        authGroup.MapGet("/oidc/callback", async (
            string provider,
            string code,
            string state,
            HttpContext httpContext,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var result = await sender.Send(
                new OidcCallbackFeature.Command(provider, code, state, ip, userAgent),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous();
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
        }).RequirePermission("identity:roles:read");

        group.MapGet("/permissions", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListPermissionsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:permissions:read");
    }

    /// <summary>Endpoints de acesso emergencial (Break Glass) — v1.1 enterprise.</summary>
    private static void MapBreakGlassEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var bgGroup = group.MapGroup("/break-glass");

        bgGroup.MapPost("/", async (
            RequestBreakGlassFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        bgGroup.MapPost("/{requestId:guid}/revoke", async (
            Guid requestId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RevokeBreakGlassFeature.Command(requestId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:revoke");

        bgGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListBreakGlassFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:read");
    }

    /// <summary>Endpoints de acesso privilegiado temporário (JIT) — v1.1 enterprise.</summary>
    private static void MapJitAccessEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var jitGroup = group.MapGroup("/jit-access");

        jitGroup.MapPost("/", async (
            RequestJitAccessFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        jitGroup.MapPost("/{requestId:guid}/decide", async (
            Guid requestId,
            DecideJitRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DecideJitAccessFeature.Command(requestId, body.Approve, body.RejectionReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:revoke");

        jitGroup.MapGet("/pending", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListJitAccessFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:read");
    }

    /// <summary>Endpoints de delegação formal de permissões — v1.1 enterprise.</summary>
    private static void MapDelegationEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var delGroup = group.MapGroup("/delegations");

        delGroup.MapPost("/", async (
            CreateDelegationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        delGroup.MapPost("/{delegationId:guid}/revoke", async (
            Guid delegationId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RevokeDelegationFeature.Command(delegationId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:revoke");

        delGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListDelegationsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");
    }

    /// <summary>Endpoints de tenant — listagem de tenants do usuário autenticado.</summary>
    private static void MapTenantEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapGet("/tenants/mine", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListMyTenantsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();

        group.MapPost("/auth/select-tenant", async (
            SelectTenantRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SelectTenantFeature.Command(request.TenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();
    }

    private sealed record SelectTenantRequest(Guid TenantId);
    private sealed record AssignRoleRequest(Guid TenantId, Guid RoleId);
    private sealed record DecideJitRequest(bool Approve, string? RejectionReason);

    /// <summary>
    /// Endpoints de recertificação de acessos (Access Review) — compliance enterprise.
    ///
    /// Fluxo principal:
    /// 1. Admin inicia uma campanha (POST /access-reviews)
    /// 2. Reviewers listam campanhas abertas (GET /access-reviews)
    /// 3. Reviewers consultam itens pendentes (GET /access-reviews/{id})
    /// 4. Reviewers decidem item a item (POST /access-reviews/{id}/items/{itemId}/decide)
    /// </summary>
    private static void MapAccessReviewEndpoints(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var reviewGroup = group.MapGroup("/access-reviews");

        // Inicia nova campanha de revisão — requer permissão de admin de usuários
        reviewGroup.MapPost("/", async (
            StartAccessReviewFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/identity/access-reviews/{0}", localizer);
        }).RequirePermission("identity:users:write");

        // Lista campanhas abertas do tenant — requer leitura de usuários
        reviewGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListAccessReviewFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Detalhe completo de uma campanha com seus itens
        reviewGroup.MapGet("/{campaignId:guid}", async (
            Guid campaignId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAccessReviewFeature.Query(campaignId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Registra decisão sobre um item de revisão (confirmar ou revogar acesso)
        reviewGroup.MapPost("/{campaignId:guid}/items/{itemId:guid}/decide", async (
            Guid campaignId,
            Guid itemId,
            DecideAccessReviewRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DecideAccessReviewItemFeature.Command(campaignId, itemId, body.Approve, body.Comment),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization();
    }

    private sealed record DecideAccessReviewRequest(bool Approve, string? Comment);
}
