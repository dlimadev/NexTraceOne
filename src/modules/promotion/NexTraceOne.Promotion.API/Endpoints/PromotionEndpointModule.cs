using Microsoft.AspNetCore.Builder;
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
        _ = app.MapGroup("/api/v1/promotion");

        // TODO: Mapear endpoints de cada feature
    }
}
