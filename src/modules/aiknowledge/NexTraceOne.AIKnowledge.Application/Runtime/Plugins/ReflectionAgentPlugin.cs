using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ReflectionAgent;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Reflection Agent.
/// </summary>
public sealed class ReflectionAgentPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<ReflectionAgentPlugin> _logger;

    public override string PluginName => "ReflectionAgent";
    public override string Description => "Executes complex tasks with iterative planning, execution, and reflection loops until quality threshold is met.";

    public ReflectionAgentPlugin(ISender sender, ILogger<ReflectionAgentPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("execute")]
    [Description("Executes a complex task using reflection loops. Returns the final output after iterative improvement.")]
    public async Task<string> ExecuteAsync(
        [Description("The task to accomplish")] string task,
        [Description("Optional preferred agent hint")] string? preferredAgent = null,
        [Description("Maximum reflection iterations (1-10, default 3)")] int? maxIterations = null)
    {
        _logger.LogInformation("SK Plugin: ReflectionAgent.execute for task: {Task}", task[..Math.Min(50, task.Length)]);
        var result = await _sender.Send(new ReflectionAgent.Command(task, preferredAgent, maxIterations));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Final Output (score {r.FinalScore}/100, {r.IterationCount} iterations):\n{r.FinalOutput}\n"
                + $"Was revised: {r.WasRevised} | Session: {r.SessionId}";
        }

        return $"Error: {result.Error.Message}";
    }
}
