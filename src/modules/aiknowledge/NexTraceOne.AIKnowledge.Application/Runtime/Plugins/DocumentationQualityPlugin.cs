using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DocumentationQuality;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Documentation Quality Agent.
/// </summary>
public sealed class DocumentationQualityPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<DocumentationQualityPlugin> _logger;

    public override string PluginName => "DocumentationQuality";
    public override string Description => "Evaluates documentation quality and detects gaps in projects.";

    public DocumentationQualityPlugin(ISender sender, ILogger<DocumentationQualityPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("evaluate")]
    [Description("Evaluates documentation quality of a project. Returns coverage, quality score, gaps, and recommendations.")]
    public async Task<string> EvaluateAsync(
        [Description("The project path to evaluate")] string projectPath)
    {
        _logger.LogInformation("SK Plugin: DocumentationQuality.evaluate for {Path}", projectPath);
        var result = await _sender.Send(new DocumentationQuality.Command(projectPath));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Coverage: {r.CoveragePercentage:F1}% ({r.DocumentedItems}/{r.TotalDocumentableItems})\n"
                + $"Quality Score: {r.QualityScore:F1}\n"
                + $"Gaps: {r.Gaps.Count}\n"
                + $"Recommendations: {string.Join(", ", r.Recommendations.Take(3))}";
        }

        return $"Error: {result.Error.Message}";
    }
}
