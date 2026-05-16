using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DocAgent;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Doc Agent.
/// </summary>
public sealed class DocAgentPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<DocAgentPlugin> _logger;

    public override string PluginName => "DocAgent";
    public override string Description => "Generates and improves technical documentation using AI.";

    public DocAgentPlugin(ISender sender, ILogger<DocAgentPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("generate")]
    [Description("Generates documentation for the provided content. Returns structured documentation with quality score and suggestions.")]
    public async Task<string> GenerateAsync(
        [Description("The content to document")] string content,
        [Description("The type of documentation (e.g., API, Guide, README)")] string docType,
        [Description("Optional target audience")] string? targetAudience = null,
        [Description("Optional writing style")] string? style = null)
    {
        _logger.LogInformation("SK Plugin: DocAgent.generate for docType={DocType}", docType);
        var result = await _sender.Send(new DocAgent.Command(content, docType, targetAudience, style));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Quality Score: {r.QualityScore}\n\n"
                + $"Generated Doc:\n{r.GeneratedDoc}\n\n"
                + $"Suggestions: {string.Join(", ", r.Suggestions)}";
        }

        return $"Error: {result.Error.Message}";
    }
}
