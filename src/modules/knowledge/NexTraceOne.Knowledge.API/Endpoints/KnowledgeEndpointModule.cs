using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace NexTraceOne.Knowledge.API.Endpoints;

/// <summary>
/// Endpoints do Knowledge Hub — gestão de documentos de conhecimento,
/// notas operacionais e relações entre objectos de conhecimento e outros contextos.
///
/// Módulo nativo de Knowledge. Ownership real do módulo Knowledge.
/// As rotas /api/v1/knowledge pertencem ao módulo Knowledge.
///
/// P10.1: Endpoint module mínimo — endpoints CRUD serão adicionados em P10.2.
/// </summary>
public sealed class KnowledgeEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var knowledge = app.MapGroup("/api/v1/knowledge");

        // P10.1: Endpoint de health-check mínimo do módulo Knowledge.
        // Endpoints CRUD completos serão adicionados em P10.2.
        knowledge.MapGet("/status", () =>
            Results.Ok(new { module = "Knowledge", status = "active", version = "10.1" }))
            .WithTags("Knowledge")
            .WithSummary("Knowledge module status check");
    }
}
