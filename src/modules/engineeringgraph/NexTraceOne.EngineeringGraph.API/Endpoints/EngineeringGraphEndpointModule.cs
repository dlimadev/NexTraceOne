using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.EngineeringGraph.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo EngineeringGraph.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class EngineeringGraphEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/engineeringgraph")
            .WithTags("EngineeringGraph");

        // TODO: Mapear endpoints de cada feature
    }
}
