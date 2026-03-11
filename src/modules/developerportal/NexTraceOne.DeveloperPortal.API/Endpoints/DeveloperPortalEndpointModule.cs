using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.DeveloperPortal.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo DeveloperPortal.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class DeveloperPortalEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/developerportal")
            .WithTags("DeveloperPortal");

        // TODO: Mapear endpoints de cada feature
    }
}
