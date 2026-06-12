using FluentValidation;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.IncidentResponder;

/// <summary>
/// Agente especializado para resposta a incidentes.
/// Correlaciona incidentes com mudanças recentes, analisa telemetria,
/// sugere root cause e recomenda mitigação.
/// </summary>
public static class IncidentResponder
{
    public sealed record Command(
        string IncidentDescription,
        string? ServiceName = null,
        int? TimeRangeHours = 24) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentDescription).NotEmpty().MaximumLength(5000);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.TimeRangeHours).InclusiveBetween(1, 168).When(x => x.TimeRangeHours.HasValue);
        }
    }

    public sealed record Response(
        string RootCause,
        string Severity,
        List<RelatedChange> RelatedChanges,
        List<MitigationStep> MitigationSteps,
        List<string> RecommendedRunbooks,
        string EstimatedMttr,
        bool EscalationRecommended);

    public sealed record RelatedChange(
        string ChangeId,
        string Description,
        string TimeAgo,
        string RiskLevel);

    public sealed record MitigationStep(
        int Priority,
        string Action,
        string ExpectedOutcome,
        string ResponsibleTeam);

    internal sealed class Handler(
        IAiKernelService kernelService,
        IAiExecutionGateway aiExecutionGateway,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var plan = await aiExecutionGateway.PreviewExecutionAsync(
                new AiExecutionRequest(
                    FeatureKey: "aiknowledge.agent.incident-responder",
                    RequestType: "agent"),
                cancellationToken);

            if (!plan.IsAvailable)
            {
                return Error.Business("AI.NotAvailable", plan.UnavailabilityReason ?? "IA indisponível.");
            }

            try
            {
                var kernel = kernelService.CreateKernel(plan.ProviderId, plan.ModelId);
                var groundingQuery = $"{request.IncidentDescription} {request.ServiceName}".Trim();
                kernel.Data["GroundingQuery"] = groundingQuery;

                var systemPrompt = string.Format("""
                    You are an expert Incident Responder for NexTraceOne. Analyze the incident and provide structured response.
                    Service: {0}
                    Time range: last {1} hours

                    Respond ONLY with valid JSON. No markdown, no explanations.

                    Expected JSON format:
                    {{
                      "rootCause": "Brief root cause description",
                      "severity": "Critical|High|Medium|Low",
                      "relatedChanges": [
                        {{
                          "changeId": "CHG-123",
                          "description": "Deployment of v2.5.0",
                          "timeAgo": "2 hours ago",
                          "riskLevel": "High"
                        }}
                      ],
                      "mitigationSteps": [
                        {{
                          "priority": 1,
                          "action": "Rollback deployment",
                          "expectedOutcome": "Restore service stability",
                          "responsibleTeam": "SRE"
                        }}
                      ],
                      "recommendedRunbooks": ["Incident-Response-DB-Outage", "Rollback-Procedure"],
                      "estimatedMttr": "30 minutes",
                      "escalationRecommended": true
                    }}
                    """, request.ServiceName ?? "Unknown", request.TimeRangeHours ?? 24);

                var messages = new List<ChatMessage>
                {
                    new("user", request.IncidentDescription)
                };

                var response = await kernelService.ExecuteChatAsync(
                    kernel, systemPrompt, messages, cancellationToken);

                if (!LlmJsonParser.TryParse<IncidentResponseLlmOutput>(response, out var output) || output is null)
                {
                    logger.LogWarning("Failed to parse incident response JSON. Raw: {Raw}", response[..Math.Min(200, response.Length)]);
                    return CreateFallbackResponse(request);
                }

                var relatedChanges = output.RelatedChanges?.Select(c => new RelatedChange(
                    c.ChangeId ?? "unknown",
                    c.Description ?? "No description",
                    c.TimeAgo ?? "unknown",
                    c.RiskLevel ?? "unknown")).ToList() ?? new List<RelatedChange>();

                var mitigationSteps = output.MitigationSteps?.Select(m => new MitigationStep(
                    m.Priority,
                    m.Action ?? "No action",
                    m.ExpectedOutcome ?? "Unknown",
                    m.ResponsibleTeam ?? "SRE")).OrderBy(m => m.Priority).ToList() ?? new List<MitigationStep>();

                return new Response(
                    output.RootCause ?? "Unable to determine root cause",
                    output.Severity ?? "Unknown",
                    relatedChanges,
                    mitigationSteps,
                    output.RecommendedRunbooks ?? new List<string>(),
                    output.EstimatedMttr ?? "Unknown",
                    output.EscalationRecommended);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Incident responder failed for service {Service}", request.ServiceName);
                return CreateFallbackResponse(request);
            }
        }

        private static Result<Response> CreateFallbackResponse(Command request)
        {
            return new Response(
                "Unable to determine root cause — manual investigation required",
                "Unknown",
                new List<RelatedChange>(),
                new List<MitigationStep>
                {
                    new(1, "Check service health dashboards", "Confirm scope of impact", "SRE"),
                    new(2, "Review recent deployments", "Identify potential triggers", "Platform"),
                    new(3, "Escalate to on-call engineer", "Get human expertise involved", "SRE")
                },
                new List<string> { "Incident-Response-Generic", "Escalation-Procedure" },
                "Unknown",
                true);
        }
    }

    private sealed class IncidentResponseLlmOutput
    {
        public string? RootCause { get; set; }
        public string? Severity { get; set; }
        public List<RelatedChangeOutput>? RelatedChanges { get; set; }
        public List<MitigationStepOutput>? MitigationSteps { get; set; }
        public List<string>? RecommendedRunbooks { get; set; }
        public string? EstimatedMttr { get; set; }
        public bool EscalationRecommended { get; set; }
    }

    private sealed class RelatedChangeOutput
    {
        public string? ChangeId { get; set; }
        public string? Description { get; set; }
        public string? TimeAgo { get; set; }
        public string? RiskLevel { get; set; }
    }

    private sealed class MitigationStepOutput
    {
        public int Priority { get; set; }
        public string? Action { get; set; }
        public string? ExpectedOutcome { get; set; }
        public string? ResponsibleTeam { get; set; }
    }
}
