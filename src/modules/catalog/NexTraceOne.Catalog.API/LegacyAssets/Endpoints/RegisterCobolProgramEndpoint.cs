using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterCobolProgramFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCobolProgram.RegisterCobolProgram;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de um novo programa COBOL no catálogo legacy.
/// Route: POST /api/catalog/legacy/cobol-programs
/// </summary>
public static class RegisterCobolProgramEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/cobol-programs", async (
            RegisterCobolProgramFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/catalog/legacy/cobol-programs/{r.Id}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
