using System.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Features.AIAgents.ServiceAnalyst;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Plugins;

/// <summary>
/// Plugin Semantic Kernel para Service Analyst Agent.
/// </summary>
public sealed class ServiceAnalystPlugin : SemanticKernelPluginBase
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceAnalystPlugin> _logger;

    public override string PluginName => "ServiceAnalyst";
    public override string Description => "Analyzes service health, identifies bottlenecks, critical dependencies and recommends improvements.";

    public ServiceAnalystPlugin(ISender sender, ILogger<ServiceAnalystPlugin> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [KernelFunction("analyze")]
    [Description("Analyzes a service description and metrics to provide health assessment, bottlenecks, dependencies and recommendations.")]
    public async Task<string> AnalyzeAsync(
        [Description("Description of the service and its current state")] string serviceDescription,
        [Description("Optional service name")] string? serviceName = null,
        [Description("Optional metrics snapshot (JSON or plain text)")] string? metricsSnapshot = null)
    {
        _logger.LogInformation("SK Plugin: ServiceAnalyst.analyze for {Service}", serviceName ?? "unnamed");
        var result = await _sender.Send(new ServiceAnalyst.Command(serviceDescription, serviceName, metricsSnapshot));

        if (result.IsSuccess)
        {
            var r = result.Value;
            var deps = r.CriticalDependencies.Any()
                ? $"Critical Dependencies: {string.Join(", ", r.CriticalDependencies.Select(d => d.DependencyName))}\n"
                : string.Empty;
            var recs = r.Recommendations.Any()
                ? $"Top Recommendations: {string.Join("; ", r.Recommendations.OrderBy(x => x.Priority).Take(3).Select(rec => $"#{rec.Priority} {rec.Action}"))}"
                : string.Empty;
            return $"Status: {r.OverallStatus} | Health Score: {r.HealthScore}/100\n"
                + $"Bottlenecks: {r.Bottlenecks.Count}\n"
                + deps
                + recs;
        }

        return $"Error: {result.Error.Message}";
    }
}
