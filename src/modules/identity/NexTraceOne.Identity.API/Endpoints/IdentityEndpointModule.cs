using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.Identity.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Identity.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class IdentityEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity")
            .WithTags("Identity");

        // TODO: Mapear endpoints de cada feature
    }
}
