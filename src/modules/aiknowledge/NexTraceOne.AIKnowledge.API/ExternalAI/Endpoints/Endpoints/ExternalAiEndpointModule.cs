using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using QueryExternalAISimpleFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple.QueryExternalAISimple;
using QueryExternalAIAdvancedFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced.QueryExternalAIAdvanced;

namespace NexTraceOne.AIKnowledge.API.ExternalAI.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo ExternalAi.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Política de autorização:
/// - Escrita: "ai:runtime:write" para endpoints de execução de queries de IA.
/// </summary>
public sealed class ExternalAiEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapQueryEndpoints(app);
    }

    private static void MapQueryEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/externalai/query");

        group.MapPost("/simple", async (
            QueryExternalAISimpleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/advanced", async (
            QueryExternalAIAdvancedFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }
}
