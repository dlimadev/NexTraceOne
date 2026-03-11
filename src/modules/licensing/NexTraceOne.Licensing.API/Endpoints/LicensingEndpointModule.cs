using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.Licensing.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Licensing.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class LicensingEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/licensing")
            .WithTags("Licensing");

        // TODO: Mapear endpoints de cada feature
    }
}
