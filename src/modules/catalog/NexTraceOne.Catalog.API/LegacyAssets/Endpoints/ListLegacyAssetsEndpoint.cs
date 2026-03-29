using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

using ListLegacyAssetsFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.ListLegacyAssets.ListLegacyAssets;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para listagem filtrada de ativos legacy do catálogo.
/// Route: GET /api/catalog/legacy/assets
/// </summary>
public static class ListLegacyAssetsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/assets", async (
            string? teamName,
            string? domain,
            Criticality? criticality,
            LifecycleStatus? lifecycleStatus,
            string? searchTerm,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListLegacyAssetsFeature.Query(
                teamName, domain, criticality, lifecycleStatus, searchTerm);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:legacy-assets:read");
    }
}
