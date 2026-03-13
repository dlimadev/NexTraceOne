using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.RuntimeIntelligence.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo RuntimeIntelligence.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class RuntimeIntelligenceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        _ = app.MapGroup("/api/v1/runtimeintelligence");

        // TODO: Mapear endpoints de cada feature
    }
}
