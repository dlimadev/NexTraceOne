using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using StartAccessReviewFeature = NexTraceOne.IdentityAccess.Application.Features.StartAccessReviewCampaign.StartAccessReviewCampaign;
using ListAccessReviewFeature = NexTraceOne.IdentityAccess.Application.Features.ListAccessReviewCampaigns.ListAccessReviewCampaigns;
using GetAccessReviewFeature = NexTraceOne.IdentityAccess.Application.Features.GetAccessReviewCampaign.GetAccessReviewCampaign;
using DecideAccessReviewItemFeature = NexTraceOne.IdentityAccess.Application.Features.DecideAccessReviewItem.DecideAccessReviewItem;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// Endpoints de recertificação de acessos (Access Review) — compliance enterprise.
///
/// Fluxo principal:
/// 1. Admin inicia uma campanha (POST /access-reviews)
/// 2. Reviewers listam campanhas abertas (GET /access-reviews)
/// 3. Reviewers consultam itens pendentes (GET /access-reviews/{id})
/// 4. Reviewers decidem item a item (POST /access-reviews/{id}/items/{itemId}/decide)
/// </summary>
internal static class AccessReviewEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de access review no subgrupo <c>/access-reviews</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var reviewGroup = group.MapGroup("/access-reviews");

        // Inicia nova campanha de revisão — requer permissão de admin de usuários
        reviewGroup.MapPost("/", async (
            StartAccessReviewFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/identity/access-reviews/{r.CampaignId}", localizer);
        }).RequirePermission("identity:users:write");

        // Lista campanhas abertas do tenant — requer leitura de usuários
        reviewGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListAccessReviewFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Detalhe completo de uma campanha com seus itens
        reviewGroup.MapGet("/{campaignId:guid}", async (
            Guid campaignId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAccessReviewFeature.Query(campaignId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:read");

        // Registra decisão sobre um item de revisão (confirmar ou revogar acesso)
        reviewGroup.MapPost("/{campaignId:guid}/items/{itemId:guid}/decide", async (
            Guid campaignId,
            Guid itemId,
            DecideAccessReviewRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DecideAccessReviewItemFeature.Command(campaignId, itemId, body.Approve, body.Comment),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:users:write");
    }

    /// <summary>
    /// DTO para decisão sobre um item de revisão de acesso (confirmar ou revogar).
    /// </summary>
    internal sealed record DecideAccessReviewRequest(bool Approve, string? Comment);
}
