using Microsoft.AspNetCore.Builder;

namespace NexTraceOne.AIKnowledge.API.Orchestration.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AiOrchestration.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class AiOrchestrationEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        _ = app.MapGroup("/api/v1/aiorchestration");

        // TODO: Mapear endpoints de cada feature
    }
}
