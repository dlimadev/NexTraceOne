using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.Knowledge.Contracts;

namespace NexTraceOne.Knowledge.API.Endpoints;

/// <summary>
/// Endpoints do Knowledge Hub — gestão de documentos de conhecimento,
/// notas operacionais e relações entre objectos de conhecimento e outros contextos.
///
/// Módulo nativo de Knowledge. Ownership real do módulo Knowledge.
/// As rotas /api/v1/knowledge pertencem ao módulo Knowledge.
///
/// P10.1: Endpoint module mínimo — endpoints CRUD serão adicionados em P10.2.
/// P10.2: Adicionado endpoint de pesquisa /api/v1/knowledge/search.
/// </summary>
public sealed class KnowledgeEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var knowledge = app.MapGroup("/api/v1/knowledge");

        // P10.1: Endpoint de health-check mínimo do módulo Knowledge.
        knowledge.MapGet("/status", () =>
            Results.Ok(new { module = "Knowledge", status = "active", version = "10.2" }))
            .WithTags("Knowledge")
            .WithSummary("Knowledge module status check");

        // P10.2: Endpoint de pesquisa no Knowledge Hub.
        knowledge.MapGet("/search", async (
            string q,
            string? scope,
            int? maxResults,
            IKnowledgeSearchProvider searchProvider,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length > 200)
                return Results.BadRequest(new { error = "Search term is required and must be at most 200 characters." });

            var max = maxResults is > 0 and <= 100 ? maxResults.Value : 25;
            var results = await searchProvider.SearchAsync(q, scope, max, cancellationToken);
            return Results.Ok(new
            {
                items = results,
                totalResults = results.Count
            });
        })
        .WithTags("Knowledge")
        .WithSummary("Search knowledge documents and operational notes");
    }
}
