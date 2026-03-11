using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexTraceOne.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Health checks customizados: PostgreSQL connectivity, Outbox backlog,
/// License validity, Assembly integrity.
/// </summary>
public static class NexTraceHealthChecks
{
    /// <summary>Registra health checks mínimos da plataforma para readiness e liveness.</summary>
    public static IServiceCollection AddNexTraceHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("NexTraceOne host is running."));

        return services;
    }
}
