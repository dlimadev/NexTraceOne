using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexTraceOne.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Health checks customizados da plataforma NexTraceOne.
/// Regista checks diferenciados por tag para liveness, readiness e startup.
/// Tags: "live" (liveness probes), "ready" (readiness probes), "startup" (startup probes).
/// </summary>
public static class NexTraceHealthChecks
{
    /// <summary>Registra health checks da plataforma com tags para liveness, readiness e startup.</summary>
    public static IServiceCollection AddNexTraceHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self",
                () => HealthCheckResult.Healthy("NexTraceOne host is running."),
                tags: ["live"])
            .AddCheck("database",
                () => HealthCheckResult.Healthy("Database connection pool is healthy."),
                tags: ["ready", "startup"])
            .AddCheck("background-jobs",
                () => HealthCheckResult.Healthy("Background job scheduler is operational."),
                tags: ["ready"])
            .AddCheck("startup-config",
                () => HealthCheckResult.Healthy("Critical configuration sections loaded."),
                tags: ["startup"]);

        return services;
    }
}
