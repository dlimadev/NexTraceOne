using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Correlation;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.EventHandlers;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Analytics;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;
using NexTraceOne.Integrations.Contracts;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence;

/// <summary>
/// Registra serviços de infraestrutura do módulo ChangeIntelligence.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo ChangeIntelligence ao container DI.</summary>
    public static IServiceCollection AddChangeIntelligenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("ChangeIntelligenceDatabase", "NexTraceOne");

        services.AddDbContext<ChangeIntelligenceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ChangeIntelligenceDbContext>());
        services.AddScoped<IChangeIntelligenceUnitOfWork>(sp => sp.GetRequiredService<ChangeIntelligenceDbContext>());
        services.AddScoped<IReleaseRepository, ReleaseRepository>();
        services.AddScoped<IBlastRadiusRepository, BlastRadiusRepository>();
        services.AddScoped<IChangeScoreRepository, ChangeScoreRepository>();
        services.AddScoped<IChangeEventRepository, ChangeEventRepository>();
        services.AddScoped<IExternalMarkerRepository, ExternalMarkerRepository>();
        services.AddScoped<IFreezeWindowRepository, FreezeWindowRepository>();
        services.AddScoped<IReleaseBaselineRepository, ReleaseBaselineRepository>();
        services.AddScoped<IObservationWindowRepository, ObservationWindowRepository>();
        services.AddScoped<IPostReleaseReviewRepository, PostReleaseReviewRepository>();
        services.AddScoped<IRollbackAssessmentRepository, RollbackAssessmentRepository>();
        services.AddScoped<IFeatureFlagStateRepository, FeatureFlagStateRepository>();
        services.AddScoped<ICanaryRolloutRepository, CanaryRolloutRepository>();
        services.AddScoped<IChangeConfidenceBreakdownRepository, ChangeConfidenceBreakdownRepository>();
        services.AddScoped<IChangeConfidenceEventRepository, ChangeConfidenceEventRepository>();
        services.AddScoped<IReleaseNotesRepository, ReleaseNotesRepository>();
        services.AddScoped<IPromotionGateRepository, PromotionGateRepository>();
        services.AddScoped<IPromotionGateEvaluationRepository, PromotionGateEvaluationRepository>();
        services.AddScoped<IExternalChangeRequestRepository, EfExternalChangeRequestRepository>();
        services.AddScoped<IReleaseContextSurface, ReleaseContextSurface>();

        // Wave D.2 — Cross-tenant Benchmarks
        services.AddScoped<ITenantBenchmarkConsentRepository, TenantBenchmarkConsentRepository>();
        services.AddScoped<IBenchmarkSnapshotRepository, BenchmarkSnapshotRepository>();

        // Wave F.1 — Release Calendar
        services.AddScoped<IReleaseCalendarRepository, ReleaseCalendarRepository>();

        // Wave F.2 — Risk Center
        services.AddScoped<IServiceRiskProfileRepository, ServiceRiskProfileRepository>();

        // Default honest-null reader para Promotion Readiness Delta.
        // Uma bridge real p/ OperationalIntelligence substitui este default na composition root.
        services.AddScoped<IRuntimeComparisonReader, Services.NullRuntimeComparisonReader>();

        // Wave T.1 — Post-Incident Learning Report (honest-null; bridge Knowledge → CG na composition root)
        services.AddScoped<IIncidentLearningReader, Services.NullIncidentLearningReader>();

        // Wave U.1 — Compliance Coverage Matrix Report (honest-null; bridge per-service compliance on composition root)
        services.AddScoped<IComplianceServiceCoverageReader, Services.NullComplianceServiceCoverageReader>();
        services.AddScoped<IIntegrationEventHandler<IncidentCreatedIntegrationEvent>, IncidentCreatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.IngestionPayloadProcessedIntegrationEvent>, IngestionPayloadProcessedIntegrationEventHandler>();

        // Phase 2: Commit Pool & Work Item Association
        services.AddScoped<ICommitAssociationRepository, CommitAssociationRepository>();
        services.AddScoped<IWorkItemAssociationRepository, WorkItemAssociationRepository>();

        // Phase 3: External Approval Gateway
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
        services.AddScoped<IReleaseApprovalPolicyRepository, ReleaseApprovalPolicyRepository>();
        services.AddHttpClient("ExternalApprovalWebhook")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IExternalApprovalWebhookSender, ExternalApprovalWebhookSender>();

        // Analytics writer: correlated traces → Elasticsearch chg_trace_release_mapping
        // Graceful degradation via NullAnalyticsWriter when Analytics:Enabled = false
        services.AddBuildingBlocksAnalytics(configuration);
        services.AddScoped<ITraceCorrelationWriter, TraceCorrelationAnalyticsWriter>();

        // Cross-module public interface — outros módulos consomem IChangeIntelligenceModule
        services.AddScoped<IChangeIntelligenceModule, ChangeIntelligenceModule>();

        // Wave AI.1 — honest-null bridge para environment instability (OI)
        services.AddScoped<IEnvironmentInstabilityReader, NullEnvironmentInstabilityReader>();

        // Wave AJ.1 — honest-null bridge para cross-tenant maturity dimensions
        services.AddScoped<ICrossTenantMaturityReader, Services.NullCrossTenantMaturityReader>();

        // Wave AJ.2 — honest-null bridge para tenant health pillar data
        services.AddScoped<ITenantHealthDataReader, Services.NullTenantHealthDataReader>();

        // Wave AJ.3 — honest-null bridge para policy evaluation history (IA)
        services.AddScoped<IPolicyEvaluationHistoryReader, Services.NullPolicyEvaluationHistoryReader>();

        // Wave AP.1 — honest-null bridge para approval workflow reader
        services.AddScoped<IApprovalWorkflowReader, Services.NullApprovalWorkflowReader>();

        // Wave AP.2 — honest-null bridge para peer review coverage
        services.AddScoped<IPeerReviewCoverageReader, Services.NullPeerReviewCoverageReader>();

        // Wave AP.3 — honest-null bridge para governance escalation
        services.AddScoped<IGovernanceEscalationReader, Services.NullGovernanceEscalationReader>();

        // Distributed signal correlation and promotion risk signals for AI-assisted analysis
        services.AddScoped<IDistributedSignalCorrelationService, DistributedSignalCorrelationService>();
        services.AddScoped<IPromotionRiskSignalProvider, PromotionRiskSignalProvider>();

        // Wave AU — Platform Self-Optimization & Adaptive Intelligence
        services.AddScoped<IConfigurationDriftReader, Services.NullConfigurationDriftReader>();
        services.AddScoped<IPlatformHealthIndexReader, Services.NullPlatformHealthIndexReader>();
        services.AddScoped<IAdaptiveRecommendationReader, Services.NullAdaptiveRecommendationReader>();

        return services;
    }
}
