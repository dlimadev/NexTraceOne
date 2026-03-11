using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.Promotion.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Promotion.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class PromotionEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/promotion")
            .WithTags("Promotion");

        // TODO: Mapear endpoints de cada feature
    }
}
