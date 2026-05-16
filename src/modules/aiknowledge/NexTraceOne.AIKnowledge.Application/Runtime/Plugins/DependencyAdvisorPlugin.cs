using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.DependencyAdvisor;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Dependency Advisor Agent.
/// </summary>
public sealed class DependencyAdvisorPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<DependencyAdvisorPlugin> _logger;

    public override string PluginName => "DependencyAdvisor";
    public override string Description => "Analyzes project dependencies and identifies vulnerabilities and outdated packages.";

    public DependencyAdvisorPlugin(ISender sender, ILogger<DependencyAdvisorPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("analyze")]
    [Description("Analyzes dependencies of a project. Returns vulnerable, outdated packages and recommendations.")]
    public async Task<string> AnalyzeAsync(
        [Description("The project path to analyze")] string projectPath)
    {
        _logger.LogInformation("SK Plugin: DependencyAdvisor.analyze for {Path}", projectPath);
        var result = await _sender.Send(new DependencyAdvisor.Command(projectPath));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Total Dependencies: {r.TotalDependencies}\n"
                + $"Vulnerable: {r.VulnerableDependencies}, Outdated: {r.OutdatedDependencies}\n"
                + $"Recommendations: {string.Join(", ", r.Recommendations.Take(3))}";
        }

        return $"Error: {result.Error.Message}";
    }
}
