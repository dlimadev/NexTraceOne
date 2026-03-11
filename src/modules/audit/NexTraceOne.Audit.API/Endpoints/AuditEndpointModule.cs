using NexTraceOne.BuildingBlocks.Application.Extensions;

namespace NexTraceOne.Audit.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Audit.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class AuditEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit")
            .WithTags("Audit");

        // TODO: Mapear endpoints de cada feature
    }
}
