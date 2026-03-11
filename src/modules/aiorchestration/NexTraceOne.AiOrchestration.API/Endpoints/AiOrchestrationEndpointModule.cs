using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.AiOrchestration.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AiOrchestration.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class AiOrchestrationEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration")
            .WithTags("AiOrchestration");

        // TODO: Mapear endpoints de cada feature
    }
}
