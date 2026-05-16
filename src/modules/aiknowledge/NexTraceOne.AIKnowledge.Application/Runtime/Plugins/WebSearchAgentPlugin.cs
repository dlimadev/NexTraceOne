using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.WebSearchAgent;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Web Search Agent.
/// </summary>
public sealed class WebSearchAgentPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<WebSearchAgentPlugin> _logger;

    public override string PluginName => "WebSearchAgent";
    public override string Description => "Searches and summarizes information from the web using AI.";

    public WebSearchAgentPlugin(ISender sender, ILogger<WebSearchAgentPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("search")]
    [Description("Searches the web for the given query and returns a summarized answer with sources.")]
    public async Task<string> SearchAsync(
        [Description("The search query")] string query,
        [Description("Optional maximum number of results")] int? maxResults = null,
        [Description("Optional focus area")] string? focus = null)
    {
        _logger.LogInformation("SK Plugin: WebSearchAgent.search for query={Query}", query);
        var result = await _sender.Send(new WebSearchAgent.Command(query, maxResults, focus));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Answer: {r.Answer}\n"
                + $"Confidence: {r.Confidence}\n"
                + $"Sources: {string.Join(", ", r.Sources.Take(5))}\n"
                + $"Results: {r.Results.Count}";
        }

        return $"Error: {result.Error.Message}";
    }
}
