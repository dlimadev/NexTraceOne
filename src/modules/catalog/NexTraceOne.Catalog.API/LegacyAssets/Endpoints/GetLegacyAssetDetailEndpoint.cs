using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetLegacyAssetDetailFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyAssetDetail.GetLegacyAssetDetail;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para obter os detalhes de um ativo legacy pelo tipo e identificador.
/// Route: GET /api/catalog/legacy/assets/{assetType}/{assetId:guid}
/// </summary>
public static class GetLegacyAssetDetailEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/assets/{assetType}/{assetId:guid}", async (
            string assetType,
            Guid assetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLegacyAssetDetailFeature.Query(assetId, assetType);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:legacy-assets:read");
    }
}
