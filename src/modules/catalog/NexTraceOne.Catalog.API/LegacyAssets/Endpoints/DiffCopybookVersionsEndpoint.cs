using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using DiffCopybookVersionsFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.DiffCopybookVersions.DiffCopybookVersions;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para diff semântico entre duas versões de copybook COBOL.
/// Route: GET /api/catalog/legacy/copybooks/{copybookId}/diff
/// </summary>
public static class DiffCopybookVersionsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/copybooks/{copybookId:guid}/diff", async (
            Guid copybookId,
            Guid baseVersionId,
            Guid targetVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new DiffCopybookVersionsFeature.Query(
                copybookId, baseVersionId, targetVersionId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:legacy-assets:read");
    }
}
