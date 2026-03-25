using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RequestJitAccessFeature = NexTraceOne.IdentityAccess.Application.Features.RequestJitAccess.RequestJitAccess;
using DecideJitAccessFeature = NexTraceOne.IdentityAccess.Application.Features.DecideJitAccess.DecideJitAccess;
using ListJitAccessFeature = NexTraceOne.IdentityAccess.Application.Features.ListJitAccessRequests.ListJitAccessRequests;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de acesso privilegiado temporário (Just-In-Time) — funcionalidade enterprise v1.1.
/// Permite solicitar acesso JIT, decidir (aprovar/rejeitar) pedidos pendentes e
/// listar pedidos para revisão por administradores.
/// </summary>
internal static class JitAccessEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de acesso JIT no subgrupo <c>/jit-access</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var jitGroup = group.MapGroup("/jit-access");

        // Qualquer usuário autenticado pode solicitar acesso JIT. O handler valida
        // o scope e a aprovação é feita por endpoint separado com permissão específica.
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
        }).RequirePermission("identity:jit-access:decide");

        jitGroup.MapGet("/pending", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListJitAccessFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:sessions:read");
    }

    /// <summary>
    /// DTO para decisão de um pedido de acesso JIT (aprovar ou rejeitar com motivo).
    /// </summary>
    internal sealed record DecideJitRequest(bool Approve, string? RejectionReason);
}
