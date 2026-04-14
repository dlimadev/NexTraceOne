using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterMainframeSystemFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterMainframeSystem.RegisterMainframeSystem;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de um novo sistema mainframe no catálogo legacy.
/// Route: POST /api/catalog/legacy/mainframe-systems
/// </summary>
public static class RegisterMainframeSystemEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/mainframe-systems", async (
            RegisterMainframeSystemFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/catalog/legacy/mainframe-systems/{r.Id}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
