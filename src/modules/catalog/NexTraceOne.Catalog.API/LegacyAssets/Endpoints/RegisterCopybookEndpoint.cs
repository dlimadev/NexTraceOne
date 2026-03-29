using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterCopybookFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCopybook.RegisterCopybook;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de um novo copybook COBOL no catálogo legacy.
/// Route: POST /api/catalog/legacy/copybooks
/// </summary>
public static class RegisterCopybookEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/copybooks", async (
            RegisterCopybookFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/catalog/legacy/copybooks/{0}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
