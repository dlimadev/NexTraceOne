using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.AIKnowledge.Application.Features.NLPRouting.PromptRouting;

namespace NexTraceOne.AIKnowledge.API.Endpoints.NLPRouting;

/// <summary>
/// Endpoints para NLP Model Routing seguindo padrão Minimal API
/// </summary>
public sealed class NLPRoutingEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/nlp")
            .WithTags("AI Knowledge - NLP Routing")
            .RequireAuthorization();

        // POST /api/v1/nlp/route — Roteia prompt para melhor provedor LLM
        group.MapPost("/route", async (
            PromptRouter.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("RoutePrompt")
        .WithSummary("Intelligently route prompt to best LLM provider");
    }
}
