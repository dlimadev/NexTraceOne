using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Observability;

/// <summary>Registra Serilog, OpenTelemetry, Metrics e HealthChecks.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: ConfigureSerilog, AddOpenTelemetry, AddHealthChecks
        return services;
    }
}
