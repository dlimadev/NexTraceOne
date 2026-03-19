using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.CookieSession;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;
using LogoutFeature = NexTraceOne.IdentityAccess.Application.Features.Logout.Logout;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints para sessão baseada em cookies httpOnly com proteção CSRF.
///
/// CONTEXTO DE SEGURANÇA:
/// Estes endpoints implementam uma alternativa ao fluxo de Bearer token em header,
/// mais segura contra XSS porque o access token fica em cookie httpOnly (não
/// acessível por JavaScript). Em contrapartida, requer proteção CSRF porque
/// cookies são enviados automaticamente pelo browser.
///
/// PADRÃO DOUBLE-SUBMIT COOKIE (CSRF):
/// 1. Login retorna um CSRF token no body e define o cookie nxt_csrf (não-httpOnly).
/// 2. O SPA armazena o CSRF token em memória e inclui como header X-Csrf-Token
///    em cada mutation (POST/PUT/DELETE/PATCH).
/// 3. O backend valida que o header coincide com o cookie.
///
/// FEATURE FLAG:
/// Estes endpoints só são registados quando Auth:CookieSession:Enabled = true.
/// Por padrão (false), o fluxo de Bearer token continua a ser o único suportado.
/// A migração para cookies deve ser feita em rollout controlado com:
/// 1. Ativação em staging + validação end-to-end.
/// 2. Atualização do frontend para usar estes endpoints.
/// 3. Cutover em produção com monitorização ativa.
/// </summary>
internal static class CookieSessionEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de sessão cookie no grupo fornecido.
    /// Deve ser chamado apenas quando Auth:CookieSession:Enabled = true.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var sessionGroup = group.MapGroup("/cookie-session");

        // POST /auth/cookie-session — autentica e define cookies httpOnly + CSRF
        sessionGroup.MapPost("/", async (
            LocalLoginFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            IOptions<CookieSessionOptions> sessionOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToHttpResult(localizer);
            }

            var loginData = result.Value!;
            var opts = sessionOptions.Value;

            // Define access token em cookie httpOnly; armazena CSRF token no cookie legível
            var csrfToken = CsrfTokenValidator.ApplyCookies(
                httpContext.Response,
                loginData.AccessToken,
                opts);

            // Retorna dados do utilizador + CSRF token (access token NÃO é retornado no body)
            return Results.Ok(new
            {
                csrfToken,
                expiresIn = loginData.ExpiresIn,
                user = loginData.User,
            });
        }).AllowAnonymous()
          .WithName("CookieSessionLogin")
          .WithSummary("Autentica e define sessão via cookie httpOnly (modelo alternativo seguro)");

        // DELETE /auth/cookie-session — encerra a sessão e remove cookies
        sessionGroup.MapDelete("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            IOptions<CookieSessionOptions> sessionOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var opts = sessionOptions.Value;

            // Valida CSRF antes de processar o logout
            if (!CsrfTokenValidator.IsValid(httpContext, opts))
            {
                return Results.Json(new { error = "csrf_token_invalid" }, statusCode: 403);
            }

            var result = await sender.Send(new LogoutFeature.Command(), cancellationToken);
            CsrfTokenValidator.ClearCookies(httpContext.Response, opts);
            return result.ToHttpResult(localizer);
        }).RequireAuthorization()
          .WithName("CookieSessionLogout")
          .WithSummary("Encerra sessão cookie e limpa cookies de autenticação");

        // GET /auth/csrf-token — retorna um CSRF token fresco para o SPA
        sessionGroup.MapGet("/csrf-token", (
            IOptions<CookieSessionOptions> sessionOptions,
            HttpContext httpContext) =>
        {
            var opts = sessionOptions.Value;

            // Só faz sentido se houver cookie de auth ativo
            var authCookie = httpContext.Request.Cookies[opts.AccessTokenCookieName];
            if (string.IsNullOrEmpty(authCookie))
            {
                return Results.Unauthorized();
            }

            // Gera novo CSRF token e renova o cookie CSRF
            var csrfToken = CsrfTokenValidator.Generate();
            httpContext.Response.Cookies.Append(opts.CsrfCookieName, csrfToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
            });

            return Results.Ok(new { csrfToken });
        }).AllowAnonymous()
          .WithName("GetCsrfToken")
          .WithSummary("Retorna um CSRF token fresco para uso em mutations (modelo cookie)");
    }
}
