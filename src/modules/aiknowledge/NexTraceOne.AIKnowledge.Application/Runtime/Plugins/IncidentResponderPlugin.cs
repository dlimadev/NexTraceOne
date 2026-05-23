using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.IncidentResponder;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Incident Responder Agent.
/// </summary>
public sealed class IncidentResponderPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<IncidentResponderPlugin> _logger;

    public override string PluginName => "IncidentResponder";
    public override string Description => "Analyzes incidents, correlates with recent changes and recommends mitigation steps.";

    public IncidentResponderPlugin(ISender sender, ILogger<IncidentResponderPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("analyze")]
    [Description("Analyzes an incident description and returns root cause, severity, related changes, mitigation steps and escalation recommendation.")]
    public async Task<string> AnalyzeAsync(
        [Description("Description of the incident")] string incidentDescription,
        [Description("Optional service name")] string? serviceName = null,
        [Description("Optional time range in hours (default 24)")] int? timeRangeHours = null)
    {
        _logger.LogInformation("SK Plugin: IncidentResponder.analyze for incident: {Incident}", incidentDescription[..Math.Min(50, incidentDescription.Length)]);
        var result = await _sender.Send(new IncidentResponder.Command(incidentDescription, serviceName, timeRangeHours));

        if (result.IsSuccess)
        {
            var r = result.Value;
            var changes = r.RelatedChanges.Any()
                ? $"Related Changes: {string.Join(", ", r.RelatedChanges.Select(c => c.ChangeId))}\n"
                : string.Empty;
            return $"Root Cause: {r.RootCause} | Severity: {r.Severity} | MTTR: {r.EstimatedMttr}\n"
                + changes
                + $"Escalation Recommended: {r.EscalationRecommended} | Mitigation Steps: {r.MitigationSteps.Count}";
        }

        return $"Error: {result.Error.Message}";
    }
}
