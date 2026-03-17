using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListMyTenantsFeature = NexTraceOne.IdentityAccess.Application.Features.ListMyTenants.ListMyTenants;
using SelectTenantFeature = NexTraceOne.IdentityAccess.Application.Features.SelectTenant.SelectTenant;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de tenants do utilizador autenticado.
/// Permite listar os tenants aos quais o utilizador pertence e
/// selecionar o tenant ativo para a sessão corrente.
/// </summary>
internal static class TenantEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de tenant no grupo raiz do módulo Identity.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
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

    /// <summary>
    /// DTO para seleção de tenant ativo na sessão do utilizador.
    /// </summary>
    internal sealed record SelectTenantRequest(Guid TenantId);
}
