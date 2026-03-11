using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.ExternalAi.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo ExternalAi.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class ExternalAiEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/externalai")
            .WithTags("ExternalAi");

        // TODO: Mapear endpoints de cada feature
    }
}
