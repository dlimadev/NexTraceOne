using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease.GetRelease;
using ListReleasesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases.ListReleases;
using GetReleaseHistoryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseHistory.GetReleaseHistory;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de consulta de releases e histórico.
/// Suportam a navegação do catálogo de releases e a visualização
/// do histórico de deployments por API asset.
///
/// Consumidos pelo Developer Portal e pelo painel de Change Intelligence.
/// </summary>
internal static class ReleaseQueryEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de consulta de releases no grupo raiz.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapGet("/{releaseId:guid}", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetReleaseFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        group.MapGet("/", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new ListReleasesFeature.Query(apiAssetId, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        group.MapGet("/{apiAssetId:guid}/history", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new GetReleaseHistoryFeature.Query(apiAssetId, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");
    }
}
