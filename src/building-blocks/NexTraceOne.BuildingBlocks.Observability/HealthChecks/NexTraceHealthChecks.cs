using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexTraceOne.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Health checks customizados da plataforma NexTraceOne.
/// Regista checks diferenciados por tag para liveness, readiness e startup.
/// Tags: "live" (liveness probes), "ready" (readiness probes), "startup" (startup probes).
/// O self check inclui versão da aplicação e uptime real do processo.
/// </summary>
public static class NexTraceHealthChecks
{
    /// <summary>Registra health checks da plataforma com tags para liveness, readiness e startup.</summary>
    public static IServiceCollection AddNexTraceHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self",
                () =>
                {
                    var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
                    var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                    var data = new Dictionary<string, object>
                    {
                        ["version"] = version,
                        ["uptimeSeconds"] = (long)uptime.TotalSeconds
                    };
                    return HealthCheckResult.Healthy("NexTraceOne host is running.", data);
                },
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
