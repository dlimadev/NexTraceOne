using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.CostIntelligence.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo CostIntelligence.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class CostIntelligenceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/costintelligence")
            .WithTags("CostIntelligence");

        // TODO: Mapear endpoints de cada feature
    }
}
