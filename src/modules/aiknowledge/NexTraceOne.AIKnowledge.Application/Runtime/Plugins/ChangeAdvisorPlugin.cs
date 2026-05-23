using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ChangeAdvisor;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Change Advisor Agent.
/// </summary>
public sealed class ChangeAdvisorPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<ChangeAdvisorPlugin> _logger;

    public override string PluginName => "ChangeAdvisor";
    public override string Description => "Analyzes proposed changes for risk, blast radius, readiness and recommends mitigation strategies.";

    public ChangeAdvisorPlugin(ISender sender, ILogger<ChangeAdvisorPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("analyze")]
    [Description("Analyzes a proposed change and returns risk assessment, blast radius, readiness score, impact analysis and mitigation steps.")]
    public async Task<string> AnalyzeAsync(
        [Description("Description of the proposed change")] string changeDescription,
        [Description("Optional target environment (e.g., production, staging)")] string? environment = null,
        [Description("Optional change type (e.g., deployment, config, schema)")] string? changeType = null,
        [Description("Optional comma-separated list of affected services")] string? affectedServices = null)
    {
        _logger.LogInformation("SK Plugin: ChangeAdvisor.analyze for change: {Change}", changeDescription[..Math.Min(50, changeDescription.Length)]);
        var result = await _sender.Send(new ChangeAdvisor.Command(changeDescription, environment, changeType, affectedServices));

        if (result.IsSuccess)
        {
            var r = result.Value;
            var approval = r.ApprovalRecommended ? "✅ Approval recommended" : "⚠️ Approval not recommended";
            return $"Risk: {r.RiskLevel} | Blast Radius: {r.BlastRadius} | Readiness: {r.ReadinessScore}/100\n"
                + $"Impact — Users: {r.Impact.UserImpact}/5, Data: {r.Impact.DataImpact}/5, Ops: {r.Impact.OperationalImpact}/5, Compliance: {r.Impact.ComplianceImpact}/5\n"
                + $"Rollback: {r.Rollback.Complexity} complexity, estimated {r.Rollback.EstimatedTime}\n"
                + $"Mitigations: {r.Mitigations.Count} | {approval}";
        }

        return $"Error: {result.Error.Message}";
    }
}
