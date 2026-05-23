using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DependencyAdvisor;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ArchitectureFitness;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DocAgent;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DocumentationQuality;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.PRAgent;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.IncidentResponder;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ReflectionAgent;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.SecurityReview;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ServiceAnalyst;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ChangeAdvisor;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.WebSearchAgent;

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

        // POST /api/v1/ai-agents/doc-agent/generate — Gera documentação inteligente
        group.MapPost("/doc-agent/generate", async (
            DocAgent.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("GenerateDocumentation")
        .WithSummary("Generate intelligent documentation using AI");

        // POST /api/v1/ai-agents/pr-agent/review — Revisão de código / PR
        group.MapPost("/pr-agent/review", async (
            PRAgent.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ReviewPullRequest")
        .WithSummary("Review code diffs and provide structured feedback");

        // POST /api/v1/ai-agents/web-search/search — Pesquisa web inteligente
        group.MapPost("/web-search/search", async (
            WebSearchAgent.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("WebSearch")
        .WithSummary("Search and summarize information using AI");

        // POST /api/v1/ai-agents/incident-responder/analyze — Resposta a incidentes
        group.MapPost("/incident-responder/analyze", async (
            IncidentResponder.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("AnalyzeIncident")
        .WithSummary("Analyze incidents, correlate changes and recommend mitigation");

        // POST /api/v1/ai-agents/service-analyst/analyze — Análise de saúde de serviços
        group.MapPost("/service-analyst/analyze", async (
            ServiceAnalyst.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("AnalyzeServiceHealth")
        .WithSummary("Analyze service health, identify bottlenecks and critical dependencies");

        // POST /api/v1/ai-agents/change-advisor/analyze — Análise de mudanças
        group.MapPost("/change-advisor/analyze", async (
            ChangeAdvisor.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("AnalyzeChangeRisk")
        .WithSummary("Analyze change risk, blast radius, readiness and recommend mitigations");

        // POST /api/v1/ai-agents/reflection-agent/execute — Agente reflexivo
        group.MapPost("/reflection-agent/execute", async (
            ReflectionAgent.Command command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ExecuteReflectionAgent")
        .WithSummary("Execute complex tasks with iterative plan-execute-reflect loops");
    }
}
