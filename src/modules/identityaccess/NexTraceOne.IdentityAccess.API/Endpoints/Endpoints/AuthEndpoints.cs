using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;
using FederatedLoginFeature = NexTraceOne.IdentityAccess.Application.Features.FederatedLogin.FederatedLogin;
using RefreshTokenFeature = NexTraceOne.IdentityAccess.Application.Features.RefreshToken.RefreshToken;
using LogoutFeature = NexTraceOne.IdentityAccess.Application.Features.Logout.Logout;
using RevokeSessionFeature = NexTraceOne.IdentityAccess.Application.Features.RevokeSession.RevokeSession;
using GetCurrentUserFeature = NexTraceOne.IdentityAccess.Application.Features.GetCurrentUser.GetCurrentUser;
using ChangePasswordFeature = NexTraceOne.IdentityAccess.Application.Features.ChangePassword.ChangePassword;
using StartOidcLoginFeature = NexTraceOne.IdentityAccess.Application.Features.StartOidcLogin.StartOidcLogin;
using OidcCallbackFeature = NexTraceOne.IdentityAccess.Application.Features.OidcCallback.OidcCallback;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de autenticação do módulo Identity.
/// Inclui login local, login federado, refresh token, logout, revogação de sessão,
/// consulta do utilizador autenticado (/me), alteração de password e fluxo OIDC.
/// Endpoints públicos (login, federated, refresh, oidc) usam AllowAnonymous.
/// Endpoints de self-service (logout, /me, password) exigem apenas autenticação.
/// </summary>
internal static class AuthEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de autenticação no grupo <c>/auth</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
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
}
