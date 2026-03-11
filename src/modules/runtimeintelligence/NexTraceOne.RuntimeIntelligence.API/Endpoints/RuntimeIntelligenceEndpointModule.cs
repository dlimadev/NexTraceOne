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
        var group = app.MapGroup("/api/v1/runtimeintelligence")
            .WithTags("RuntimeIntelligence");

        // TODO: Mapear endpoints de cada feature
    }
}
