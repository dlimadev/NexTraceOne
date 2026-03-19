using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using AskCatalogQuestionFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.AskCatalogQuestion.AskCatalogQuestion;
using ClassifyChangeWithAIFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.ClassifyChangeWithAI.ClassifyChangeWithAI;
using SuggestSemanticVersionWithAIFeature = NexTraceOne.AIKnowledge.Application.Orchestration.Features.SuggestSemanticVersionWithAI.SuggestSemanticVersionWithAI;

namespace NexTraceOne.AIKnowledge.API.Orchestration.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AiOrchestration.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Política de autorização:
/// - Escrita: "ai:runtime:write" para endpoints de execução de orquestração de IA.
/// </summary>
public sealed class AiOrchestrationEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapCatalogEndpoints(app);
        MapChangeEndpoints(app);
        MapContractEndpoints(app);
    }

    private static void MapCatalogEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/catalog");

        group.MapPost("/ask", async (
            AskCatalogQuestionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapChangeEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/changes");

        group.MapPost("/classify", async (
            ClassifyChangeWithAIFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapContractEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/aiorchestration/contracts");

        group.MapPost("/suggest-version", async (
            SuggestSemanticVersionWithAIFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }
}
