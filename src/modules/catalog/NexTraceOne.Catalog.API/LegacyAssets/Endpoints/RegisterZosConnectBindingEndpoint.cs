using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterZosConnectBindingFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterZosConnectBinding.RegisterZosConnectBinding;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de um novo binding z/OS Connect no catálogo legacy.
/// Route: POST /api/catalog/legacy/zos-connect-bindings
/// </summary>
public static class RegisterZosConnectBindingEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/zos-connect-bindings", async (
            RegisterZosConnectBindingFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/catalog/legacy/zos-connect-bindings/{0}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
