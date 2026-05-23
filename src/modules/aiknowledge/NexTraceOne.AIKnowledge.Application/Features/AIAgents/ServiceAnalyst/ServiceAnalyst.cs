using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.ServiceAnalyst;

/// <summary>
/// Agente especializado para análise de saúde e operacionalidade de serviços.
/// Identifica gargalos, dependências críticas e sugere melhorias com base no catálogo de serviços.
/// </summary>
public static class ServiceAnalyst
{
    public sealed record Command(
        string ServiceDescription,
        string? ServiceName = null,
        string? MetricsSnapshot = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceDescription).NotEmpty().MaximumLength(5000);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.MetricsSnapshot).MaximumLength(10_000).When(x => x.MetricsSnapshot is not null);
        }
    }

    public sealed record Response(
        string OverallStatus,
        int HealthScore,
        List<Bottleneck> Bottlenecks,
        List<CriticalDependency> CriticalDependencies,
        List<Recommendation> Recommendations,
        string? Summary);

    public sealed record Bottleneck(
        string Area,
        string Description,
        string Severity,
        string? SuggestedFix);

    public sealed record CriticalDependency(
        string DependencyName,
        string DependencyType,
        string ImpactLevel,
        string? Alternative);

    public sealed record Recommendation(
        int Priority,
        string Action,
        string ExpectedBenefit,
        string Effort);

    internal sealed class Handler(
        IAiKernelService kernelService,
        IAiProviderFactory providerFactory,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var provider = providerFactory.GetChatProvider("ollama")
                ?? providerFactory.GetChatProvider("openai");

            if (provider is null)
            {
                logger.LogWarning("No AI provider available for service analysis");
                return CreateFallbackResponse(request);
            }

            try
            {
                var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);
                var groundingQuery = $"{request.ServiceName} {request.ServiceDescription}".Trim();
                kernel.Data["GroundingQuery"] = groundingQuery;

                var systemPrompt = BuildSystemPrompt(request);

                var messages = new List<ChatMessage>
                {
                    new("user", BuildUserPrompt(request))
                };

                var response = await kernelService.ExecuteChatAsync(
                    kernel, systemPrompt, messages, cancellationToken);

                if (!LlmJsonParser.TryParse<ServiceAnalysisLlmOutput>(response, out var output) || output is null)
                {
                    logger.LogWarning("Failed to parse service analysis JSON. Raw: {Raw}", response[..Math.Min(200, response.Length)]);
                    return CreateFallbackResponse(request);
                }

                var bottlenecks = output.Bottlenecks?.Select(b => new Bottleneck(
                    b.Area ?? "Unknown",
                    b.Description ?? "No description",
                    b.Severity ?? "Medium",
                    b.SuggestedFix)).ToList() ?? new List<Bottleneck>();

                var dependencies = output.CriticalDependencies?.Select(d => new CriticalDependency(
                    d.DependencyName ?? "Unknown",
                    d.DependencyType ?? "Unknown",
                    d.ImpactLevel ?? "Medium",
                    d.Alternative)).ToList() ?? new List<CriticalDependency>();

                var recommendations = output.Recommendations?.Select(r => new Recommendation(
                    r.Priority,
                    r.Action ?? "No action",
                    r.ExpectedBenefit ?? "Unknown",
                    r.Effort ?? "Unknown")).OrderBy(r => r.Priority).ToList() ?? new List<Recommendation>();

                return new Response(
                    output.OverallStatus ?? "Unknown",
                    output.HealthScore is >= 0 and <= 100 ? output.HealthScore : 50,
                    bottlenecks,
                    dependencies,
                    recommendations,
                    output.Summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Service analyst failed for service {Service}", request.ServiceName);
                return CreateFallbackResponse(request);
            }
        }

        private static string BuildSystemPrompt(Command request)
        {
            return """
                You are an expert Service Analyst for NexTraceOne. Analyze service health and operational status.
                Respond ONLY with valid JSON. No markdown, no explanations.

                Expected JSON format:
                {
                  "overallStatus": "Healthy|Degraded|Critical|Unknown",
                  "healthScore": 75,
                  "bottlenecks": [
                    {
                      "area": "Database",
                      "description": "Connection pool exhaustion detected",
                      "severity": "High",
                      "suggestedFix": "Increase max pool size or implement connection retry policy"
                    }
                  ],
                  "criticalDependencies": [
                    {
                      "dependencyName": "payment-gateway",
                      "dependencyType": "External API",
                      "impactLevel": "High",
                      "alternative": "Fallback to queued processing"
                    }
                  ],
                  "recommendations": [
                    {
                      "priority": 1,
                      "action": "Implement circuit breaker pattern",
                      "expectedBenefit": "Reduce cascading failures",
                      "effort": "Medium"
                    }
                  ],
                  "summary": "Brief executive summary of findings"
                }
                """;
        }

        private static string BuildUserPrompt(Command request)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(request.ServiceName))
                sb.AppendLine($"Service Name: {request.ServiceName}");
            sb.AppendLine($"Description: {request.ServiceDescription}");
            if (!string.IsNullOrWhiteSpace(request.MetricsSnapshot))
                sb.AppendLine($"Metrics:\n{request.MetricsSnapshot}");
            return sb.ToString();
        }

        private static Result<Response> CreateFallbackResponse(Command request)
        {
            return new Response(
                "Unknown",
                50,
                new List<Bottleneck>
                {
                    new("General", "Unable to perform automated analysis — manual review required", "Medium", null)
                },
                new List<CriticalDependency>(),
                new List<Recommendation>
                {
                    new(1, "Review service health dashboards manually", "Confirm current status", "Low"),
                    new(2, "Check dependency topology for recent changes", "Identify potential triggers", "Low"),
                    new(3, "Schedule architecture review meeting", "Deep-dive into service design", "Medium")
                },
                $"Fallback analysis for {(request.ServiceName ?? "unspecified service")}. LLM analysis unavailable.");
        }
    }

    private sealed class ServiceAnalysisLlmOutput
    {
        public string? OverallStatus { get; set; }
        public int HealthScore { get; set; }
        public List<BottleneckOutput>? Bottlenecks { get; set; }
        public List<CriticalDependencyOutput>? CriticalDependencies { get; set; }
        public List<RecommendationOutput>? Recommendations { get; set; }
        public string? Summary { get; set; }
    }

    private sealed class BottleneckOutput
    {
        public string? Area { get; set; }
        public string? Description { get; set; }
        public string? Severity { get; set; }
        public string? SuggestedFix { get; set; }
    }

    private sealed class CriticalDependencyOutput
    {
        public string? DependencyName { get; set; }
        public string? DependencyType { get; set; }
        public string? ImpactLevel { get; set; }
        public string? Alternative { get; set; }
    }

    private sealed class RecommendationOutput
    {
        public int Priority { get; set; }
        public string? Action { get; set; }
        public string? ExpectedBenefit { get; set; }
        public string? Effort { get; set; }
    }
}
