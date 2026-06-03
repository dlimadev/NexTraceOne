using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Correlation;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluatePromotionReadinessDeltaGate;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Contracts.Promotion.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Contracts.RulesetGovernance.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Contracts.Workflow.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Analytics;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.EventHandlers;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;
using NexTraceOne.ChangeGovernance.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Services;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Services;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Repositories;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Services;
using NexTraceOne.Integrations.Contracts;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

using CiPromotionGateRepository = NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories.PromotionGateRepository;
using PrmPromotionGateRepository = NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Repositories.PromotionGateRepository;
using CiIPromotionGateRepository = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions.IPromotionGateRepository;
using PrmIPromotionGateRepository = NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions.IPromotionGateRepository;

namespace NexTraceOne.ChangeGovernance.Infrastructure;

/// <summary>
/// Registra todos os serviços de infraestrutura do módulo ChangeGovernance.
/// Consolida ChangeIntelligence + Workflow + Promotion + RulesetGovernance num único DbContext e DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de infraestrutura do módulo ChangeGovernance ao container DI.</summary>
    public static IServiceCollection AddChangeGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("ChangeGovernanceDatabase", "NexTraceOne");

        services.AddDbContext<ChangeGovernanceDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ChangeGovernanceDbContext>());
        services.AddScoped<IChangeIntelligenceUnitOfWork>(sp => sp.GetRequiredService<ChangeGovernanceDbContext>());
        services.AddScoped<IWorkflowUnitOfWork>(sp => sp.GetRequiredService<ChangeGovernanceDbContext>());
        services.AddScoped<IPromotionUnitOfWork>(sp => sp.GetRequiredService<ChangeGovernanceDbContext>());
        services.AddScoped<IRulesetGovernanceUnitOfWork>(sp => sp.GetRequiredService<ChangeGovernanceDbContext>());

        // ── ChangeIntelligence: Repositórios ──────────────────────────────────
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
        services.AddScoped<CiIPromotionGateRepository, CiPromotionGateRepository>();
        services.AddScoped<IPromotionGateEvaluationRepository, PromotionGateEvaluationRepository>();
        services.AddScoped<IExternalChangeRequestRepository, EfExternalChangeRequestRepository>();
        services.AddScoped<IReleaseContextSurface, ReleaseContextSurface>();
        services.AddScoped<ITenantBenchmarkConsentRepository, TenantBenchmarkConsentRepository>();
        services.AddScoped<IBenchmarkSnapshotRepository, BenchmarkSnapshotRepository>();
        services.AddScoped<IReleaseCalendarRepository, ReleaseCalendarRepository>();
        services.AddScoped<IServiceRiskProfileRepository, ServiceRiskProfileRepository>();
        services.AddScoped<IRuntimeComparisonReader, NullRuntimeComparisonReader>();
        services.Configure<PromotionReadinessDeltaOptions>(
            configuration.GetSection(PromotionReadinessDeltaOptions.SectionKey));
        services.AddScoped<IIncidentLearningReader, NullIncidentLearningReader>();
        services.AddScoped<IComplianceServiceCoverageReader, NullComplianceServiceCoverageReader>();
        services.AddScoped<IZeroTrustServiceReader, NullZeroTrustServiceReader>();
        services.AddScoped<IIntegrationEventHandler<IncidentCreatedIntegrationEvent>, IncidentCreatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<IntegrationEvents.IngestionPayloadProcessedIntegrationEvent>, IngestionPayloadProcessedIntegrationEventHandler>();
        services.AddScoped<ICommitAssociationRepository, CommitAssociationRepository>();
        services.AddScoped<IWorkItemAssociationRepository, WorkItemAssociationRepository>();
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
        services.AddScoped<IReleaseApprovalPolicyRepository, ReleaseApprovalPolicyRepository>();
        services.AddHttpClient("ExternalApprovalWebhook")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30))
            .AddStandardResilienceHandler();
        services.AddScoped<IExternalApprovalWebhookSender, ExternalApprovalWebhookSender>();
        services.AddScoped<ITraceCorrelationWriter, TraceCorrelationAnalyticsWriter>();
        services.AddScoped<IChangeIntelligenceModule, ChangeIntelligenceModule>();
        services.AddScoped<IEnvironmentInstabilityReader, NullEnvironmentInstabilityReader>();
        services.AddScoped<ICrossTenantMaturityReader, NullCrossTenantMaturityReader>();
        services.AddScoped<ITenantHealthDataReader, NullTenantHealthDataReader>();
        services.AddScoped<IPolicyEvaluationHistoryReader, NullPolicyEvaluationHistoryReader>();
        services.AddScoped<IApprovalWorkflowReader, NullApprovalWorkflowReader>();
        services.AddScoped<IPeerReviewCoverageReader, NullPeerReviewCoverageReader>();
        services.AddScoped<IGovernanceEscalationReader, NullGovernanceEscalationReader>();
        services.AddScoped<IDistributedSignalCorrelationService, DistributedSignalCorrelationService>();
        services.AddScoped<IPromotionRiskSignalProvider, PromotionRiskSignalProvider>();
        services.AddScoped<IConfigurationDriftReader, NullConfigurationDriftReader>();
        services.AddScoped<IPlatformHealthIndexReader, NullPlatformHealthIndexReader>();
        services.AddScoped<IAdaptiveRecommendationReader, NullAdaptiveRecommendationReader>();
        services.AddScoped<ICrossStandardComplianceGapReader, NullCrossStandardComplianceGapReader>();
        services.AddScoped<IEvidenceCollectionStatusReader, NullEvidenceCollectionStatusReader>();
        services.AddScoped<IRegulatoryChangeImpactReader, NullRegulatoryChangeImpactReader>();
        services.AddScoped<IEvidencePackIntegrityReader, NullEvidencePackIntegrityReader>();
        services.AddScoped<IMultiDimensionalPromotionConfidenceReader, NullMultiDimensionalPromotionConfidenceReader>();

        // ── Workflow: Repositórios ────────────────────────────────────────────
        services.AddScoped<IWorkflowTemplateRepository, WorkflowTemplateRepository>();
        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IWorkflowStageRepository, WorkflowStageRepository>();
        services.AddScoped<IEvidencePackRepository, EvidencePackRepository>();
        services.AddScoped<IApprovalDecisionRepository, ApprovalDecisionRepository>();
        services.AddScoped<ISlaPolicyRepository, SlaPolicyRepository>();
        services.AddScoped<IWorkflowModule, WorkflowModuleService>();

        // ── Promotion: Repositórios ───────────────────────────────────────────
        services.AddScoped<IDeploymentEnvironmentRepository, DeploymentEnvironmentRepository>();
        services.AddScoped<IPromotionRequestRepository, PromotionRequestRepository>();
        services.AddScoped<PrmIPromotionGateRepository, PrmPromotionGateRepository>();
        services.AddScoped<IGateEvaluationRepository, GateEvaluationRepository>();
        services.AddScoped<IPromotionModule, PromotionModuleService>();

        // ── RulesetGovernance: Repositórios ───────────────────────────────────
        services.AddScoped<IRulesetRepository, RulesetRepository>();
        services.AddScoped<IRulesetBindingRepository, RulesetBindingRepository>();
        services.AddScoped<ILintResultRepository, LintResultRepository>();
        services.AddScoped<IRulesetGovernanceModule, RulesetGovernanceModuleService>();

        return services;
    }
}
