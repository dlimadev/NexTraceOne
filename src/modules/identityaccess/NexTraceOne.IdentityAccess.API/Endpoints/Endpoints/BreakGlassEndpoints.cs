using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RequestBreakGlassFeature = NexTraceOne.IdentityAccess.Application.Features.RequestBreakGlass.RequestBreakGlass;
using RevokeBreakGlassFeature = NexTraceOne.IdentityAccess.Application.Features.RevokeBreakGlass.RevokeBreakGlass;
using ListBreakGlassFeature = NexTraceOne.IdentityAccess.Application.Features.ListBreakGlassRequests.ListBreakGlassRequests;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

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

        // Qualquer usuário autenticado pode solicitar break glass — trata-se de acesso
        // emergencial. A auditoria e post-mortem garantem controlo. Não exige permissão
        // específica para não bloquear situações reais de emergência.
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
        }).RequirePermission("identity:break-glass:decide");

        bgGroup.MapGet("/", async (
            bool? includeInactive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListBreakGlassFeature.Query(includeInactive ?? false),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:read");
    }
}
