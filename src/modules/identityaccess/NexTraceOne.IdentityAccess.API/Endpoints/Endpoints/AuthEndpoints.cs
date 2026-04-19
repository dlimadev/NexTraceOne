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
using VerifyMfaChallengeFeature = NexTraceOne.IdentityAccess.Application.Features.VerifyMfaChallenge.VerifyMfaChallenge;
using ForgotPasswordFeature = NexTraceOne.IdentityAccess.Application.Features.ForgotPassword.ForgotPassword;
using ResetPasswordFeature = NexTraceOne.IdentityAccess.Application.Features.ResetPassword.ResetPassword;
using ActivateAccountFeature = NexTraceOne.IdentityAccess.Application.Features.ActivateAccount.ActivateAccount;
using ResendMfaCodeFeature = NexTraceOne.IdentityAccess.Application.Features.ResendMfaCode.ResendMfaCode;
using StartSamlLoginFeature = NexTraceOne.IdentityAccess.Application.Features.StartSamlLogin.StartSamlLogin;
using SamlAcsCallbackFeature = NexTraceOne.IdentityAccess.Application.Features.SamlAcsCallback.SamlAcsCallback;

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
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        authGroup.MapPost("/federated", async (
            FederatedLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        authGroup.MapPost("/refresh", async (
            RefreshTokenFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

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
        }).AllowAnonymous()
          .RequireRateLimiting("auth-sensitive");

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
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /auth/mfa/verify — segundo passo do fluxo MFA: verifica código TOTP e emite tokens completos.
        // Requer: ChallengeToken (emitido pelo /auth/login quando MfaRequired = true) + Code TOTP 6 dígitos.
        authGroup.MapPost("/mfa/verify", async (
            VerifyMfaChallengeFeature.Command command,
            HttpContext httpContext,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var enrichedCommand = command with { IpAddress = ip, UserAgent = userAgent };
            var result = await sender.Send(enrichedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /auth/forgot-password — solicitar reset de password (sempre retorna sucesso)
        authGroup.MapPost("/forgot-password", async (
            ForgotPasswordFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /auth/reset-password — reset de password com token
        authGroup.MapPost("/reset-password", async (
            ResetPasswordFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /auth/activate — activar conta com token
        authGroup.MapPost("/activate", async (
            ActivateAccountFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /auth/mfa/resend — reenviar código MFA
        authGroup.MapPost("/mfa/resend", async (
            ResendMfaCodeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // ── SAML 2.0 SSO ────────────────────────────────────────────────────────
        // GET /auth/saml/sso — inicia o fluxo SAML e redireciona para o IdP
        authGroup.MapGet("/saml/sso", async (
            string? returnUrl,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new StartSamlLoginFeature.Query(returnUrl), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToHttpResult(localizer);
            }

            return Results.Redirect(result.Value.RedirectUrl);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /auth/saml/acs — recebe o callback do IdP (Assertion Consumer Service)
        authGroup.MapPost("/saml/acs", async (
            HttpContext httpContext,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var samlResponse = httpContext.Request.Form["SAMLResponse"].ToString();
            var relayState = httpContext.Request.Form["RelayState"].ToString();
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var result = await sender.Send(
                new SamlAcsCallbackFeature.Command(samlResponse, relayState, ip, userAgent),
                cancellationToken);

            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");
    }
}
