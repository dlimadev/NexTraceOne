using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Events;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.FinOps.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Services;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Contracts.Automation.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Reliability.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Services;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.EventHandlers;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Services;
using NexTraceOne.OperationalIntelligence.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Services;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.ClickHouse;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore;

using IActiveServiceNamesReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IActiveServiceNamesReader;
using ITeamOperationalMetricsReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.ITeamOperationalMetricsReader;
using IVulnerabilityAdvisoryReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IVulnerabilityAdvisoryReader;
using IIncidentKnowledgeReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IIncidentKnowledgeReader;
using IPlatformAdoptionReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IPlatformAdoptionReader;
using IDeploymentRiskForecastReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IDeploymentRiskForecastReader;
using IErrorBudgetReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IErrorBudgetReader;
using IIncidentImpactScorecardReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IIncidentImpactScorecardReader;
using ISreMaturityReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.ISreMaturityReader;
using ITrafficAnomalyReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.ITrafficAnomalyReader;
using IEnvironmentBehaviorComparisonReader = NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions.IEnvironmentBehaviorComparisonReader;

namespace NexTraceOne.OperationalIntelligence.Infrastructure;

/// <summary>
/// Registra todos os serviços de infraestrutura do módulo OperationalIntelligence (IncidentResponse).
/// Consolida Incidents + Reliability + Automation + RuntimeIntelligence num único DbContext e DI.
/// Cost e TelemetryStore permanecem em DbContexts separados até fase de extração FinOps.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo IncidentResponse ao container DI.</summary>
    public static IServiceCollection AddIncidentResponseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("IncidentResponseDatabase", "NexTraceOne");

        services.AddDbContext<IncidentResponseDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IncidentResponseDbContext>());
        services.AddScoped<IReliabilityUnitOfWork>(sp => sp.GetRequiredService<IncidentResponseDbContext>());
        services.AddScoped<IAutomationUnitOfWork>(sp => sp.GetRequiredService<IncidentResponseDbContext>());
        services.AddScoped<IRuntimeIntelligenceUnitOfWork>(sp => sp.GetRequiredService<IncidentResponseDbContext>());

        // ── Incidents ─────────────────────────────────────────────────────────
        services.AddScoped<IIncidentStore, EfIncidentStore>();
        services.AddScoped<IIncidentContextSurface, IncidentContextSurface>();
        services.AddScoped<IOperationalAlertHandler, IncidentAlertHandler>();
        services.AddScoped<IIncidentCorrelationRepository, EfIncidentCorrelationRepository>();
        services.AddScoped<IRunbookRepository, EfRunbookRepository>();
        services.AddScoped<IRunbookExecutionRepository, EfRunbookExecutionRepository>();
        services.AddScoped<ICorrelationFeatureReader, NullCorrelationFeatureReader>();
        services.AddScoped<IPostIncidentReviewRepository, EfPostIncidentReviewRepository>();
        services.AddScoped<IMitigationWorkflowRepository, EfMitigationWorkflowRepository>();
        services.AddScoped<IMitigationValidationRepository, EfMitigationValidationRepository>();
        services.AddScoped<IIncidentNarrativeRepository, EfIncidentNarrativeRepository>();
        services.AddScoped<IChangeIntelligenceReader, EfChangeIntelligenceReader>();
        services.AddScoped<IIntegrationEventHandler<DeploymentEventReceivedEvent>, DeploymentEventReceivedHandler>();
        services.AddScoped<ILegacyEventCorrelator, LegacyEventCorrelator>();
        services.AddScoped<IIncidentModule, IncidentModuleService>();

        // ── Reliability ───────────────────────────────────────────────────────
        services.AddScoped<IReliabilitySnapshotRepository, ReliabilitySnapshotRepository>();
        services.AddScoped<IReliabilityRuntimeSurface, ReliabilityRuntimeSurface>();
        services.AddScoped<IReliabilityIncidentSurface, ReliabilityIncidentSurface>();
        services.AddScoped<ISloDefinitionRepository, SloDefinitionRepository>();
        services.AddScoped<ISlaDefinitionRepository, SlaDefinitionRepository>();
        services.AddScoped<IErrorBudgetSnapshotRepository, ErrorBudgetSnapshotRepository>();
        services.AddScoped<IBurnRateSnapshotRepository, BurnRateSnapshotRepository>();
        services.AddSingleton<IErrorBudgetCalculator, ErrorBudgetCalculator>();
        services.AddScoped<IServiceFailurePredictionRepository, ServiceFailurePredictionRepository>();
        services.AddScoped<ICapacityForecastRepository, CapacityForecastRepository>();
        services.AddScoped<IIncidentPredictionPatternRepository, IncidentPredictionPatternRepository>();
        services.AddScoped<IHealingRecommendationRepository, HealingRecommendationRepository>();
        services.AddScoped<IReliabilityModule, ReliabilityModuleService>();

        // ── Automation ────────────────────────────────────────────────────────
        services.AddScoped<IAutomationWorkflowRepository, AutomationWorkflowRepository>();
        services.AddScoped<IAutomationValidationRepository, AutomationValidationRepository>();
        services.AddScoped<IAutomationAuditRepository, AutomationAuditRepository>();
        services.AddScoped<IAutomationModule, AutomationModuleService>();

        // ── Runtime Intelligence ──────────────────────────────────────────────
        services.AddScoped<IRuntimeSnapshotRepository, RuntimeSnapshotRepository>();
        services.AddScoped<IRuntimeBaselineRepository, RuntimeBaselineRepository>();
        services.AddScoped<IDriftFindingRepository, DriftFindingRepository>();
        services.AddScoped<IObservabilityProfileRepository, ObservabilityProfileRepository>();
        services.AddScoped<IRuntimeIntelligenceModule, RuntimeIntelligenceModule>();
        services.AddScoped<ICustomChartRepository, CustomChartRepository>();
        services.AddScoped<IChaosExperimentRepository, ChaosExperimentRepository>();
        services.AddScoped<IAnomalyNarrativeRepository, AnomalyNarrativeRepository>();
        services.AddScoped<IEnvironmentDriftReportRepository, EnvironmentDriftReportRepository>();
        services.AddScoped<IOperationalPlaybookRepository, OperationalPlaybookRepository>();
        services.AddScoped<IPlaybookExecutionRepository, PlaybookExecutionRepository>();
        services.AddScoped<IResilienceReportRepository, ResilienceReportRepository>();
        services.AddScoped<IProfilingSessionRepository, ProfilingSessionRepository>();
        services.AddScoped<IServiceCostAllocationRepository, ServiceCostAllocationRepository>();
        services.AddScoped<ISloObservationRepository, SloObservationRepository>();
        services.AddScoped<IActiveServiceNamesReader, NullActiveServiceNamesReader>();
        services.AddScoped<ITeamOperationalMetricsReader, NullTeamOperationalMetricsReader>();
        services.AddScoped<IVulnerabilityAdvisoryReader, NullVulnerabilityAdvisoryReader>();
        services.AddScoped<IIncidentKnowledgeReader, NullIncidentKnowledgeReader>();
        services.AddScoped<IPlatformAdoptionReader, NexTraceOne.OperationalIntelligence.Application.Runtime.Services.NullPlatformAdoptionReader>();
        services.AddScoped<IDeploymentRiskForecastReader, NullDeploymentRiskForecastReader>();
        services.AddScoped<IErrorBudgetReader, NexTraceOne.OperationalIntelligence.Application.Runtime.NullErrorBudgetReader>();
        services.AddScoped<IIncidentImpactScorecardReader, NexTraceOne.OperationalIntelligence.Application.Runtime.NullIncidentImpactScorecardReader>();
        services.AddScoped<ISreMaturityReader, NexTraceOne.OperationalIntelligence.Application.Runtime.NullSreMaturityReader>();
        services.AddScoped<ITrafficAnomalyReader, NexTraceOne.OperationalIntelligence.Application.Runtime.NullTrafficAnomalyReader>();
        services.AddScoped<IEnvironmentBehaviorComparisonReader, NexTraceOne.OperationalIntelligence.Application.Runtime.NullEnvironmentBehaviorComparisonReader>();

        // ── Log Search — Telemetry Backend Selection (Elasticsearch ou ClickHouse) ─
        services.Configure<TelemetryStoreOptions>(
            configuration.GetSection(TelemetryStoreOptions.SectionName));

        var telemetryOptions = configuration.GetSection(TelemetryStoreOptions.SectionName).Get<TelemetryStoreOptions>();
        var backendType = telemetryOptions?.ObservabilityProvider?.BackendType?.ToLowerInvariant() ?? "elasticsearch";

        switch (backendType)
        {
            case "clickhouse":
                services.AddScoped<ITelemetrySearchService, ClickHouseLogSearchService>();
                break;
            case "elasticsearch":
            default:
                services.AddHttpClient<ITelemetrySearchService, ElasticsearchLogSearchService>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                }).AddStandardResilienceHandler();
                break;
        }

        services.AddScoped(sp => sp.GetRequiredService<ITelemetrySearchService>());

        var clickHouseConnectionString = configuration.GetConnectionString("ClickHouse")
            ?? "http://localhost:8123/default";
        services.AddSingleton<IClickHouseRepository>(new ClickHouseRepository(clickHouseConnectionString));

        // ── TelemetryStore (mantido separado até extração FinOps) ─────────────
        services.AddTelemetryStoreInfrastructure(configuration);

        return services;
    }
}
