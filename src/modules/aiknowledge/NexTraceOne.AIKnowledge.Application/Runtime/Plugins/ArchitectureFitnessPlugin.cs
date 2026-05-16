using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ArchitectureFitness;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Architecture Fitness Agent.
/// </summary>
public sealed class ArchitectureFitnessPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<ArchitectureFitnessPlugin> _logger;

    public override string PluginName => "ArchitectureFitness";
    public override string Description => "Evaluates architectural fitness, detects code smells, and suggests refactorings.";

    public ArchitectureFitnessPlugin(ISender sender, ILogger<ArchitectureFitnessPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("evaluate")]
    [Description("Evaluates the architecture of a project at the given path. Returns scores and refactoring suggestions.")]
    public async Task<string> EvaluateAsync(
        [Description("The project path to evaluate")] string projectPath)
    {
        _logger.LogInformation("SK Plugin: ArchitectureFitness.evaluate for {Path}", projectPath);
        var result = await _sender.Send(new ArchitectureFitness.Command(projectPath));

        if (result.IsSuccess)
        {
            var r = result.Value;
            return $"Overall Score: {r.OverallScore:F1}\n"
                + $"Modularity: {r.ModularityScore:F1}, Coupling: {r.CouplingScore:F1}, Cohesion: {r.CohesionScore:F1}\n"
                + $"Code Smells: {r.CodeSmells.Count}\n"
                + $"Refactoring Suggestions: {r.RefactoringSuggestions.Count}";
        }

        return $"Error: {result.Error.Message}";
    }
}
