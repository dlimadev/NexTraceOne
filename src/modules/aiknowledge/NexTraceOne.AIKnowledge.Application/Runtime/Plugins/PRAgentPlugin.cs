using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.PRAgent;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para PR Agent.
/// </summary>
public sealed class PRAgentPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<PRAgentPlugin> _logger;

    public override string PluginName => "PRAgent";
    public override string Description => "Reviews code diffs and provides structured feedback.";

    public PRAgentPlugin(ISender sender, ILogger<PRAgentPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("review")]
    [Description("Reviews a code diff and returns a structured review with comments, score, and recommendations.")]
    public async Task<string> ReviewAsync(
        [Description("The code diff to review")] string diff,
        [Description("Optional PR title")] string? title = null,
        [Description("Optional PR description")] string? description = null,
        [Description("Optional programming language")] string? language = null)
    {
        _logger.LogInformation("SK Plugin: PRAgent.review");
        var result = await _sender.Send(new PRAgent.Command(diff, title, description, language));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Overall Score: {r.OverallScore}\n"
                + $"Summary: {r.Summary}\n"
                + $"Comments: {r.Comments.Count}\n"
                + $"Recommendations: {string.Join(", ", r.Recommendations.Take(3))}";
        }

        return $"Error: {result.Error.Message}";
    }
}
