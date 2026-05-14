using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DependencyAdvisor;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ArchitectureFitness;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DocumentationQuality;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.SecurityReview;

namespace NexTraceOne.AIKnowledge.API.Endpoints.AIAgents;

/// <summary>
/// Endpoints para AI Agents seguindo padrão Minimal API do projeto
/// </summary>
public sealed class AiAgentsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai-agents")
            .WithTags("AI Knowledge - AI Agents")
            .RequireAuthorization();

        // POST /api/v1/ai-agents/dependency-advisor/analyze — Analisa dependências do projeto
        group.MapPost("/dependency-advisor/analyze", async (
            DependencyAdvisor.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("AnalyzeDependencies")
        .WithSummary("Analyze project dependencies and identify vulnerabilities");

        // POST /api/v1/ai-agents/architecture-fitness/evaluate — Avalia qualidade arquitetural
        group.MapPost("/architecture-fitness/evaluate", async (
            ArchitectureFitness.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("EvaluateArchitecture")
        .WithSummary("Evaluate architectural fitness and detect code smells");

        // POST /api/v1/ai-agents/documentation-quality/evaluate — Avalia qualidade da documentação
        group.MapPost("/documentation-quality/evaluate", async (
            DocumentationQuality.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("EvaluateDocumentation")
        .WithSummary("Evaluate documentation quality and completeness");

        // POST /api/v1/ai-agents/security-review/scan — Scan de segurança
        group.MapPost("/security-review/scan", async (
            SecurityReview.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ScanSecurity")
        .WithSummary("Scan for security vulnerabilities and compliance issues");
    }
}
