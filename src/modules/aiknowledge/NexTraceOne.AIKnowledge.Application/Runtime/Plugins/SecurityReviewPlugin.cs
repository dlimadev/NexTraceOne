using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.SecurityReview;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Security Review Agent.
/// </summary>
public sealed class SecurityReviewPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<SecurityReviewPlugin> _logger;

    public override string PluginName => "SecurityReview";
    public override string Description => "Analyzes code and project paths for security vulnerabilities and compliance issues.";

    public SecurityReviewPlugin(ISender sender, ILogger<SecurityReviewPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("scan")]
    [Description("Scans a project path for security vulnerabilities. Returns a structured report with vulnerabilities, compliance issues, and recommendations.")]
    public async Task<string> ScanAsync(
        [Description("The project path to scan for vulnerabilities")] string projectPath)
    {
        _logger.LogInformation("SK Plugin: SecurityReview.scan for {Path}", projectPath);
        var result = await _sender.Send(new SecurityReview.Command(projectPath));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Security Score: {r.OverallSecurityScore:F1}\n"
                + $"Vulnerabilities: {r.TotalVulnerabilities} (Critical: {r.CriticalCount}, High: {r.HighCount})\n"
                + $"Compliance Issues: {r.ComplianceIssues.Count}\n"
                + $"Recommendations: {string.Join(", ", r.Recommendations.Take(3))}";
        }

        return $"Error: {result.Error.Message}";
    }
}
