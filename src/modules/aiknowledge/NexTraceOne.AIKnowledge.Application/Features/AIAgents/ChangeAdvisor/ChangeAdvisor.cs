using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.ChangeAdvisor;

/// <summary>
/// Agente especializado para análise de mudanças e inteligência de change management.
/// Avalia risco, blast radius, readiness e sugere estratégias de mitigação e rollback.
/// </summary>
public static class ChangeAdvisor
{
    public sealed record Command(
        string ChangeDescription,
        string? Environment = null,
        string? ChangeType = null,
        string? AffectedServices = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChangeDescription).NotEmpty().MaximumLength(5000);
            RuleFor(x => x.Environment).MaximumLength(100).When(x => x.Environment is not null);
            RuleFor(x => x.ChangeType).MaximumLength(100).When(x => x.ChangeType is not null);
            RuleFor(x => x.AffectedServices).MaximumLength(2000).When(x => x.AffectedServices is not null);
        }
    }

    public sealed record Response(
        string RiskLevel,
        string BlastRadius,
        int ReadinessScore,
        ImpactAssessment Impact,
        RollbackStrategy Rollback,
        bool ApprovalRecommended,
        List<Mitigation> Mitigations,
        string? Summary);

    public sealed record ImpactAssessment(
        int UserImpact,
        int DataImpact,
        int OperationalImpact,
        int ComplianceImpact,
        string? DetailedAnalysis);

    public sealed record RollbackStrategy(
        string EstimatedTime,
        string Complexity,
        List<string> Prerequisites,
        string? Procedure);

    public sealed record Mitigation(
        int Priority,
        string Action,
        string TargetArea,
        string ExpectedOutcome);

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
                logger.LogWarning("No AI provider available for change advisory");
                return CreateFallbackResponse(request);
            }

            try
            {
                var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);
                var groundingQuery = $"{request.ChangeType} {request.ChangeDescription}".Trim();
                kernel.Data["GroundingQuery"] = groundingQuery;

                var systemPrompt = """
                    You are an expert Change Advisor for NexTraceOne. Analyze the proposed change and provide structured intelligence.
                    Respond ONLY with valid JSON. No markdown, no explanations.

                    Expected JSON format:
                    {
                      "riskLevel": "Low|Medium|High|Critical",
                      "blastRadius": "Isolated|Team|Department|Organization-Wide",
                      "readinessScore": 75,
                      "impact": {
                        "userImpact": 3,
                        "dataImpact": 2,
                        "operationalImpact": 4,
                        "complianceImpact": 1,
                        "detailedAnalysis": "Detailed impact description"
                      },
                      "rollback": {
                        "estimatedTime": "15 minutes",
                        "complexity": "Low|Medium|High",
                        "prerequisites": ["Database snapshot", "Feature flag enabled"],
                        "procedure": "Step-by-step rollback description"
                      },
                      "approvalRecommended": true,
                      "mitigations": [
                        {
                          "priority": 1,
                          "action": "Enable feature flags for gradual rollout",
                          "targetArea": "Deployment",
                          "expectedOutcome": "Reduce blast radius"
                        }
                      ],
                      "summary": "Brief executive summary"
                    }
                    """;

                var messages = new List<ChatMessage>
                {
                    new("user", BuildUserPrompt(request))
                };

                var response = await kernelService.ExecuteChatAsync(
                    kernel, systemPrompt, messages, cancellationToken);

                if (!LlmJsonParser.TryParse<ChangeAnalysisLlmOutput>(response, out var output) || output is null)
                {
                    logger.LogWarning("Failed to parse change analysis JSON. Raw: {Raw}", response[..Math.Min(200, response.Length)]);
                    return CreateFallbackResponse(request);
                }

                var impact = output.Impact is not null
                    ? new ImpactAssessment(
                        Clamp(output.Impact.UserImpact, 0, 5),
                        Clamp(output.Impact.DataImpact, 0, 5),
                        Clamp(output.Impact.OperationalImpact, 0, 5),
                        Clamp(output.Impact.ComplianceImpact, 0, 5),
                        output.Impact.DetailedAnalysis)
                    : new ImpactAssessment(0, 0, 0, 0, null);

                var rollback = output.Rollback is not null
                    ? new RollbackStrategy(
                        output.Rollback.EstimatedTime ?? "Unknown",
                        output.Rollback.Complexity ?? "Unknown",
                        output.Rollback.Prerequisites ?? new List<string>(),
                        output.Rollback.Procedure)
                    : new RollbackStrategy("Unknown", "Unknown", new List<string>(), null);

                var mitigations = output.Mitigations?.Select(m => new Mitigation(
                    m.Priority,
                    m.Action ?? "No action",
                    m.TargetArea ?? "General",
                    m.ExpectedOutcome ?? "Unknown")).OrderBy(m => m.Priority).ToList() ?? new List<Mitigation>();

                return new Response(
                    output.RiskLevel ?? "Unknown",
                    output.BlastRadius ?? "Unknown",
                    output.ReadinessScore is >= 0 and <= 100 ? output.ReadinessScore : 50,
                    impact,
                    rollback,
                    output.ApprovalRecommended,
                    mitigations,
                    output.Summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Change advisor failed for change: {Change}", request.ChangeDescription[..Math.Min(100, request.ChangeDescription.Length)]);
                return CreateFallbackResponse(request);
            }
        }

        private static string BuildUserPrompt(Command request)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(request.ChangeType))
                sb.AppendLine($"Change Type: {request.ChangeType}");
            if (!string.IsNullOrWhiteSpace(request.Environment))
                sb.AppendLine($"Target Environment: {request.Environment}");
            if (!string.IsNullOrWhiteSpace(request.AffectedServices))
                sb.AppendLine($"Affected Services: {request.AffectedServices}");
            sb.AppendLine($"Change Description: {request.ChangeDescription}");
            return sb.ToString();
        }

        private static Result<Response> CreateFallbackResponse(Command request)
        {
            return new Response(
                "Unknown",
                "Unknown",
                50,
                new ImpactAssessment(0, 0, 0, 0, "Unable to assess impact — manual review required."),
                new RollbackStrategy("Unknown", "Unknown", new List<string>(), null),
                false,
                new List<Mitigation>
                {
                    new(1, "Schedule manual change review with architecture team", "Governance", "Ensure change safety"),
                    new(2, "Prepare rollback plan before deployment", "Operations", "Minimize recovery time"),
                    new(3, "Validate change in non-production environment first", "Testing", "Catch issues early")
                },
                "Fallback change analysis. LLM analysis unavailable — proceed with caution.");
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }

    private sealed class ChangeAnalysisLlmOutput
    {
        public string? RiskLevel { get; set; }
        public string? BlastRadius { get; set; }
        public int ReadinessScore { get; set; }
        public ImpactOutput? Impact { get; set; }
        public RollbackOutput? Rollback { get; set; }
        public bool ApprovalRecommended { get; set; }
        public List<MitigationOutput>? Mitigations { get; set; }
        public string? Summary { get; set; }
    }

    private sealed class ImpactOutput
    {
        public int UserImpact { get; set; }
        public int DataImpact { get; set; }
        public int OperationalImpact { get; set; }
        public int ComplianceImpact { get; set; }
        public string? DetailedAnalysis { get; set; }
    }

    private sealed class RollbackOutput
    {
        public string? EstimatedTime { get; set; }
        public string? Complexity { get; set; }
        public List<string>? Prerequisites { get; set; }
        public string? Procedure { get; set; }
    }

    private sealed class MitigationOutput
    {
        public int Priority { get; set; }
        public string? Action { get; set; }
        public string? TargetArea { get; set; }
        public string? ExpectedOutcome { get; set; }
    }
}
