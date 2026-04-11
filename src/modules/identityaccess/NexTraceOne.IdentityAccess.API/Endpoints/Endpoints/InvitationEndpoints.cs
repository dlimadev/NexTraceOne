using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using GetInvitationDetailsFeature = NexTraceOne.IdentityAccess.Application.Features.GetInvitationDetails.GetInvitationDetails;
using AcceptInvitationFeature = NexTraceOne.IdentityAccess.Application.Features.AcceptInvitation.AcceptInvitation;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de convites do módulo Identity.
/// Permite consultar detalhes de um convite por token e aceitar o convite.
/// Ambos os endpoints são públicos (AllowAnonymous) com rate limiting.
/// </summary>
internal static class InvitationEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var invGroup = group.MapGroup("/invitations");

        // GET /invitations/{token} — consultar detalhes de um convite
        invGroup.MapGet("/{token}", async (
            string token,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetInvitationDetailsFeature.Query(token);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");

        // POST /invitations/accept — aceitar um convite
        invGroup.MapPost("/accept", async (
            AcceptInvitationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous()
          .RequireRateLimiting("auth");
    }
}
