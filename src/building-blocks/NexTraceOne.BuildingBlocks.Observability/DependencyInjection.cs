using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.Metrics;
using NexTraceOne.BuildingBlocks.Observability.Tracing;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NexTraceOne.BuildingBlocks.Observability;

/// <summary>Registra Serilog, OpenTelemetry, Metrics e HealthChecks.</summary>
public static class DependencyInjection
{
    /// <summary>Registra observabilidade base compartilhada da plataforma.</summary>
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("NexTraceOne"))
            .WithTracing(tracing => tracing
                .AddSource(
                    NexTraceActivitySources.Commands.Name,
                    NexTraceActivitySources.Queries.Name,
                    NexTraceActivitySources.Events.Name,
                    NexTraceActivitySources.ExternalHttp.Name)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter("NexTraceOne")
                .AddOtlpExporter());

        services.AddNexTraceHealthChecks();

        return services;
    }
}
