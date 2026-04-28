using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.CookieSession;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ListMyTenantsFeature = NexTraceOne.IdentityAccess.Application.Features.ListMyTenants.ListMyTenants;
using ProvisionTenantFeature = NexTraceOne.IdentityAccess.Application.Features.ProvisionTenant.ProvisionTenant;
using SelectTenantFeature = NexTraceOne.IdentityAccess.Application.Features.SelectTenant.SelectTenant;
using ListTenantsFeature = NexTraceOne.IdentityAccess.Application.Features.ListTenants.ListTenants;
using GetTenantFeature = NexTraceOne.IdentityAccess.Application.Features.GetTenant.GetTenant;
using CreateTenantFeature = NexTraceOne.IdentityAccess.Application.Features.CreateTenant.CreateTenant;
using UpdateTenantFeature = NexTraceOne.IdentityAccess.Application.Features.UpdateTenant.UpdateTenant;
using DeactivateTenantFeature = NexTraceOne.IdentityAccess.Application.Features.DeactivateTenant.DeactivateTenant;
using ActivateTenantFeature = NexTraceOne.IdentityAccess.Application.Features.ActivateTenant.ActivateTenant;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de tenants do utilizador autenticado e de administração da plataforma.
/// Permite listar os tenants aos quais o utilizador pertence e selecionar o tenant ativo.
/// Endpoints de administração permitem gerir todos os tenants (Platform Admin exclusivo).
/// </summary>
internal static class TenantEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de tenant no grupo raiz do módulo Identity.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        // ── Endpoints do utilizador autenticado ────────────────────────────

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
            IOptions<CookieSessionOptions> sessionOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SelectTenantFeature.Command(request.TenantId), cancellationToken);

            if (!result.IsSuccess)
            {
                return result.ToHttpResult(localizer);
            }

            var response = result.Value!;
            var opts = sessionOptions.Value;
            var authCookie = httpContext.Request.Cookies[opts.AccessTokenCookieName];

            if (opts.Enabled && !string.IsNullOrWhiteSpace(authCookie))
            {
                var csrfToken = CsrfTokenValidator.ApplyCookies(httpContext.Response, response.AccessToken, opts);

                return Results.Ok(new
                {
                    response.AccessToken,
                    response.ExpiresIn,
                    response.TenantId,
                    response.TenantName,
                    response.RoleName,
                    response.Permissions,
                    csrfToken
                });
            }

            return Results.Ok(response);
        }).RequireAuthorization();

        // ── Endpoints administrativos (Platform Admin) ─────────────────────

        // Lista todos os tenants com paginação e pesquisa
        group.MapGet("/admin/tenants", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? search,
            bool? isActive,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(
                new ListTenantsFeature.Query(search, isActive, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenants:admin");

        // Obtém detalhe de um tenant
        group.MapGet("/admin/tenants/{tenantId:guid}", async (
            Guid tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetTenantFeature.Query(tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenants:admin");

        // Cria um novo tenant
        group.MapPost("/admin/tenants", async (
            CreateTenantFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenants:admin");

        // Actualiza dados de um tenant
        group.MapPut("/admin/tenants/{tenantId:guid}", async (
            Guid tenantId,
            UpdateTenantFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { TenantId = tenantId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenants:admin");

        // Desactiva um tenant
        group.MapDelete("/admin/tenants/{tenantId:guid}", async (
            Guid tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateTenantFeature.Command(tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenants:admin");

        // Reactiva um tenant desactivado
        group.MapPatch("/admin/tenants/{tenantId:guid}/activate", async (
            Guid tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ActivateTenantFeature.Command(tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenants:admin");

        // SaaS-05: Provisiona novo tenant com licença — wizard de onboarding
        group.MapPost("/admin/tenants/provision", async (
            ProvisionTenantFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/identity/admin/tenants/{r.TenantId}", localizer);
        }).RequirePermission("identity:tenants:admin");
    }

    /// <summary>
    /// DTO para seleção de tenant ativo na sessão do utilizador.
    /// </summary>
    internal sealed record SelectTenantRequest(Guid TenantId);
}

