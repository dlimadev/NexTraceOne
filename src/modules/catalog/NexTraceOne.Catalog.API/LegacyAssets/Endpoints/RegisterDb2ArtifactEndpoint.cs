using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterDb2ArtifactFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterDb2Artifact.RegisterDb2Artifact;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de um novo artefacto DB2 no catálogo legacy.
/// Route: POST /api/catalog/legacy/db2-artifacts
/// </summary>
public static class RegisterDb2ArtifactEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/db2-artifacts", async (
            RegisterDb2ArtifactFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/catalog/legacy/db2-artifacts/{0}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
