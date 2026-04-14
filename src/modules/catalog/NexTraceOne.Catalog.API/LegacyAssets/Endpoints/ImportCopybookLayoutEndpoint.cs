using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ImportCopybookLayoutFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.ImportCopybookLayout.ImportCopybookLayout;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para importação de layout de copybook COBOL com criação de versão.
/// Route: POST /api/catalog/legacy/copybooks/{copybookId}/import
/// </summary>
public static class ImportCopybookLayoutEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/copybooks/{copybookId:guid}/import", async (
            Guid copybookId,
            ImportCopybookLayoutRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ImportCopybookLayoutFeature.Command(
                copybookId, request.CopybookText, request.VersionLabel);
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/catalog/legacy/copybooks/{r.CopybookId}/versions", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }

    /// <summary>Request body para importação de layout de copybook.</summary>
    public sealed record ImportCopybookLayoutRequest(string CopybookText, string VersionLabel);
}
