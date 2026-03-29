using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using SyncLegacyAssetsFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.SyncLegacyAssets.SyncLegacyAssets;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para importação bulk de ativos legacy via API de ingestão.
/// Route: POST /api/catalog/legacy/assets/sync
/// </summary>
public static class SyncLegacyAssetsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/assets/sync", async (
            SyncLegacyAssetsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
