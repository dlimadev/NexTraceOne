using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;

namespace NexTraceOne.ApiHost;

internal static class ApiHostHealthChecks
{
    internal static IServiceCollection AddApiHostOperationalHealthChecks(this IServiceCollection services)
    {
        var healthChecks = services.AddHealthChecks();

        healthChecks
            .AddCheck<DbContextConnectivityHealthCheck<IdentityDbContext>>("identity-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<CatalogGraphDbContext>>("catalog-graph-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<ContractsDbContext>>("contracts-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<ChangeIntelligenceDbContext>>("change-intelligence-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<RuntimeIntelligenceDbContext>>("runtime-intelligence-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<GovernanceDbContext>>("governance-db", HealthStatus.Unhealthy, ["ready", "health"])
            .AddCheck<DbContextConnectivityHealthCheck<RulesetGovernanceDbContext>>("ruleset-governance-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<WorkflowDbContext>>("workflow-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<PromotionDbContext>>("promotion-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<DeveloperPortalDbContext>>("developer-portal-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<IncidentDbContext>>("incident-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<CostIntelligenceDbContext>>("cost-intelligence-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<AuditDbContext>>("audit-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<AiGovernanceDbContext>>("ai-governance-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<ExternalAiDbContext>>("external-ai-db", HealthStatus.Unhealthy, ["health"])
            .AddCheck<DbContextConnectivityHealthCheck<AiOrchestrationDbContext>>("ai-orchestration-db", HealthStatus.Unhealthy, ["health"])
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

            var data = new Dictionary<string, object?>
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
