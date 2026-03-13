using Microsoft.AspNetCore.Builder;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using RequestBreakGlassFeature = NexTraceOne.Identity.Application.Features.RequestBreakGlass.RequestBreakGlass;
using RevokeBreakGlassFeature = NexTraceOne.Identity.Application.Features.RevokeBreakGlass.RevokeBreakGlass;
using ListBreakGlassFeature = NexTraceOne.Identity.Application.Features.ListBreakGlassRequests.ListBreakGlassRequests;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Endpoints de acesso emergencial (Break Glass) — funcionalidade enterprise v1.1.
/// Permite solicitar acesso emergencial, revogar pedidos existentes e listar
/// todos os pedidos de break glass para auditoria e controlo.
/// </summary>
internal static class BreakGlassEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de break glass no subgrupo <c>/break-glass</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
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
}
