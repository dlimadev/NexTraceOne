using Microsoft.AspNetCore.Builder;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListEnvironmentsFeature = NexTraceOne.Identity.Application.Features.ListEnvironments.ListEnvironments;
using GrantEnvironmentAccessFeature = NexTraceOne.Identity.Application.Features.GrantEnvironmentAccess.GrantEnvironmentAccess;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Endpoints de gestão de ambientes (Environment) do módulo Identity.
///
/// Ambientes representam estágios do ciclo de vida (Development, Pre-Production, Production)
/// e permitem autorização granular — um utilizador pode ter acesso de escrita em Development
/// mas apenas leitura em Production.
///
/// A separação entre Tenant (organização/cliente) e Environment (estágio operacional)
/// é fundamental para a governança enterprise do NexTraceOne.
/// </summary>
internal static class EnvironmentEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de ambiente no subgrupo <c>/environments</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var envGroup = group.MapGroup("/environments");

        // Lista ambientes ativos do tenant — qualquer utilizador autenticado pode visualizar
        envGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListEnvironmentsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Concede acesso a um ambiente para um utilizador — requer permissão administrativa
        envGroup.MapPost("/access", async (
            GrantEnvironmentAccessFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");
    }
}
