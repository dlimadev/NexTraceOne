using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.HealthChecks;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Persistence;

namespace NexTraceOne.ApiHost;

internal static class ApiHostHealthChecks
{
    internal static IServiceCollection AddApiHostOperationalHealthChecks(this IServiceCollection services)
    {
        var healthChecks = services.AddHealthChecks();

        healthChecks
            .AddCheck<DbContextConnectivityHealthCheck<IdentityDbContext>>("identity-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<ServiceCatalogDbContext>>("service-catalog-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<ChangeGovernanceDbContext>>("change-governance-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<IncidentResponseDbContext>>("incident-response-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<PlatformGovernanceDbContext>>("governance-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<CostIntelligenceDbContext>>("cost-intelligence-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<AiHubDbContext>>("ai-hub-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<AiProvidersHealthCheck>("ai-providers", HealthStatus.Degraded, ["health"]);

        return services;
    }

    private sealed class AiProvidersHealthCheck(IAiProviderHealthService healthService) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var results = await healthService.CheckAllProvidersAsync(cancellationToken);
            var totalProviders = results.Count;
            var unhealthyProviders = results
                .Where(result => !result.IsHealthy)
                .Select(result => result.ProviderId)
                .ToArray();

            var data = new Dictionary<string, object>
            {
                ["totalProviders"] = totalProviders,
                ["unhealthyProviders"] = unhealthyProviders
            };

            if (totalProviders == 0)
            {
                return HealthCheckResult.Healthy("No AI providers are registered.", data);
            }

            return unhealthyProviders.Length == 0
                ? HealthCheckResult.Healthy("All AI providers are healthy.", data)
                : new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "One or more AI providers are unavailable.",
                    data: data);
        }
    }
}
