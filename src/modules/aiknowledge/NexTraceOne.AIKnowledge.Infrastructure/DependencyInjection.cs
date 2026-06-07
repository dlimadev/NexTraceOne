using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.HandleModelFeedbackThresholdExceeded;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Application.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Services;
using NexTraceOne.AIKnowledge.Contracts.ExternalAI.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Contracts.Governance.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.AIKnowledge.Contracts.Orchestration.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Infrastructure.Context;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Repositories;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Services;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.EventHandlers;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.HealthChecks;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.ClickHouse;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Services;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AIKnowledge.Infrastructure;

/// <summary>
/// Registra todos os serviços de infraestrutura do módulo AIHub.
/// Consolida AiGovernance + ExternalAi + AiOrchestration num único DbContext e DI.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiHubInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("AiHubDatabase", "NexTraceOne");

        services.AddDbContext<AiHubDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);

            if (string.Equals(
                Environment.GetEnvironmentVariable("NEXTRACE_IGNORE_PENDING_MODEL_CHANGES"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            {
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>(),
                serviceProvider.GetRequiredService<TenantRlsInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AiHubDbContext>());

        // ── AiGovernance: Repositórios ────────────────────────────────────────
        services.AddScoped<IAiAccessPolicyRepository, AiAccessPolicyRepository>();
        services.AddScoped<IAiModelRepository, AiModelRepository>();
        services.AddScoped<IAiBudgetRepository, AiBudgetRepository>();
        services.AddScoped<IAiAssistantConversationRepository, AiAssistantConversationRepository>();
        services.AddScoped<IAiMessageRepository, AiMessageRepository>();
        services.AddScoped<IAiUsageEntryRepository, AiUsageEntryRepository>();
        services.AddScoped<IAiKnowledgeSourceRepository, AiKnowledgeSourceRepository>();
        services.AddScoped<IAiIdeClientRegistrationRepository, AiIdeClientRegistrationRepository>();
        services.AddScoped<IAiIdeCapabilityPolicyRepository, AiIdeCapabilityPolicyRepository>();
        services.AddScoped<IAiRoutingDecisionRepository, AiRoutingDecisionRepository>();
        services.AddScoped<IAiRoutingStrategyRepository, AiRoutingStrategyRepository>();
        services.AddScoped<IAiProviderRepository, AiProviderRepository>();
        services.AddScoped<IAiSourceRepository, AiSourceRepository>();
        services.AddScoped<IAiTokenQuotaPolicyRepository, AiTokenQuotaPolicyRepository>();
        services.AddScoped<IAiTokenUsageLedgerRepository, AiTokenUsageLedgerRepository>();
        services.AddScoped<IAiExternalInferenceRecordRepository, AiExternalInferenceRecordRepository>();
        services.AddScoped<IAiAgentRepository, AiAgentRepository>();
        services.AddScoped<IAiAgentExecutionRepository, AiAgentExecutionRepository>();
        services.AddScoped<IAiAgentArtifactRepository, AiAgentArtifactRepository>();
        services.AddScoped<IAiKnowledgeSourceWeightRepository, AiKnowledgeSourceWeightRepository>();
        services.AddScoped<IAiToolDefinitionRepository, AiToolDefinitionRepository>();
        services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
        services.AddScoped<IAiGuardrailRepository, AiGuardrailRepository>();
        services.AddScoped<IAiEvaluationRepository, AiEvaluationRepository>();
        services.AddScoped<IAiFeedbackRepository, AiFeedbackRepository>();
        services.AddScoped<IOnboardingSessionRepository, OnboardingSessionRepository>();
        services.AddScoped<IIdeQuerySessionRepository, IdeQuerySessionRepository>();
        services.AddScoped<IAiModelAuthorizationService, AiModelAuthorizationService>();
        services.AddScoped<IAiExecutionPlanRepository, AiExecutionPlanRepository>();
        services.AddScoped<AiAgentRuntimeService>();
        services.AddScoped<IAiAgentRuntimeService>(sp =>
            new AiAgentRuntimeServiceObservabilityDecorator(sp.GetRequiredService<AiAgentRuntimeService>()));
        services.AddScoped<IAgentWorkflowOrchestrator, AgentWorkflowOrchestrator>();
        services.AddScoped<IWorkflowReplanningService, AdaptiveWorkflowReplanningService>();
        services.AddScoped<IAiSkillRepository, AiSkillRepository>();
        services.AddScoped<IAiSkillExecutionRepository, AiSkillExecutionRepository>();
        services.AddScoped<IAiSkillFeedbackRepository, AiSkillFeedbackRepository>();
        services.AddScoped<IAiAgentTrajectoryFeedbackRepository, AiAgentTrajectoryFeedbackRepository>();
        services.AddScoped<IAiAgentPerformanceMetricRepository, AiAgentPerformanceMetricRepository>();
        services.AddHostedService<TrajectoryExporterJob>();
        services.AddScoped<ISkillLoader, SkillLoader>();
        services.AddScoped<ISkillRegistry, SkillRegistry>();
        services.AddScoped<ISkillContextInjector, SkillContextInjector>();
        services.AddScoped<ISkillExecutor, SkillExecutorService>();
        services.AddScoped<IAiWarRoomRepository, WarRoomRepository>();
        services.AddScoped<IAiChangeConfidenceRepository, ChangeConfidenceRepository>();
        services.AddScoped<IGuardianAlertRepository, GuardianAlertRepository>();
        services.AddScoped<IOrganizationalMemoryRepository, OrganizationalMemoryRepository>();
        services.AddHostedService<ProactiveArchitectureGuardianJob>();
        services.AddScoped<ISelfHealingActionRepository, SelfHealingActionRepository>();
        services.AddScoped<IEvaluationSuiteRepository, EvaluationSuiteRepository>();
        services.AddScoped<IEvaluationCaseRepository, EvaluationCaseRepository>();
        services.AddScoped<IEvaluationRunRepository, EvaluationRunRepository>();
        services.AddScoped<IEvaluationDatasetRepository, EvaluationDatasetRepository>();
        services.AddScoped<IAiEvalDatasetRepository, AiEvalDatasetRepository>();
        services.AddScoped<IAiEvalRunRepository, AiEvalRunRepository>();
        services.AddScoped<HandleModelFeedbackThresholdExceededHandler>();
        services.AddScoped<
            IIntegrationEventHandler<ModelFeedbackThresholdExceededIntegrationEvent>,
            ModelFeedbackThresholdExceededEventHandlerAdapter>();
        services.AddScoped<IAiGovernanceModule, AiGovernanceModuleService>();
        services.AddScoped<IAgentExecutionPlanRepository, EfAgentExecutionPlanRepository>();
        services.AddScoped<IModelRoutingPolicyRepository, EfModelRoutingPolicyRepository>();
        services.AddScoped<IPromptIntentClassifier, NexTraceOne.AIKnowledge.Application.Governance.Services.PromptIntentClassifierService>();
        services.AddScoped<IModelPredictionRepository, EfModelPredictionRepository>();
        services.AddScoped<IAiFeatureModelBindingRepository, AiFeatureModelBindingRepository>();
        services.AddScoped<IPromptAssetRepository, PromptAssetRepository>();
        services.AddScoped<IExternalDataSourceRepository, ExternalDataSourceRepository>();
        services.AddScoped<IDataSourceSyncService, DataSourceSyncService>();
        services.AddSingleton<IDataSourceConnectorFactory, DataSourceConnectorFactory>();
        services.AddSingleton<IDataSourceConnector, BraveSearchConnector>();
        services.AddSingleton<IDataSourceConnector, GitHubConnector>();
        services.AddSingleton<IDataSourceConnector, GitLabConnector>();
        services.AddSingleton<IDataSourceConnector, LocalDirectoryConnector>();
        services.AddSingleton<IDataSourceConnector, CustomHttpConnector>();
        services.AddHttpClient("BraveSearch")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddStandardResilienceHandler();
        services.AddHttpClient("GitHubConnector")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddStandardResilienceHandler();
        services.AddHttpClient("GitLabConnector")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddStandardResilienceHandler();
        services.AddHttpClient("CustomHttpConnector")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddStandardResilienceHandler();
        services.AddHostedService<FeedbackThresholdJob>();
        services.AddHostedService<AiDataRetentionJob>();
        services.AddHostedService<EmbeddingIndexJob>();
        services.AddHostedService<QdrantIndexJob>();
        services.AddHostedService<ExternalDataSourceSyncJob>();

        // ── ExternalAi: Repositórios ──────────────────────────────────────────
        services.AddScoped<IKnowledgeCaptureRepository, KnowledgeCaptureRepository>();
        services.AddScoped<IExternalAiConsultationRepository, ExternalAiConsultationRepository>();
        services.AddScoped<IExternalAiPolicyRepository, ExternalAiPolicyRepository>();
        services.AddScoped<IExternalAiProviderRepository, ExternalAiProviderRepository>();
        services.AddScoped<IExternalAiModule, ExternalAiModule>();

        // ── AiOrchestration: Repositórios ─────────────────────────────────────
        services.AddScoped<IAIContextBuilder, AIContextBuilder>();
        services.AddScoped<IPromotionRiskContextBuilder, PromotionRiskContextBuilder>();
        services.AddScoped<IAiContextRepository, AiContextRepository>();
        services.AddScoped<IAiOrchestrationConversationRepository, AiOrchestrationConversationRepository>();
        services.AddScoped<IKnowledgeCaptureEntryRepository, KnowledgeCaptureEntryRepository>();
        services.AddScoped<IGeneratedTestArtifactRepository, GeneratedTestArtifactRepository>();
        services.AddScoped<IAgentWorkflowExecutionRepository, AgentWorkflowExecutionRepository>();
        services.AddScoped<IAiOrchestrationModule, AiOrchestrationModule>();

        // ── Analytics & Search: ClickHouse analytics + busca semântica via pgvector ─
        // Elasticsearch removido — IAiSearchRepository usa NullAiSearchRepository permanentemente.
        var clickHouseConnectionString = configuration.GetConnectionString("AiAnalytics");

        if (!string.IsNullOrEmpty(clickHouseConnectionString))
        {
            var chOptions = ClickHouseConnectionOptions.FromConnectionString(clickHouseConnectionString);
            services.AddSingleton(chOptions);

            services.AddHttpClient("ClickHouseAiAnalytics", client =>
            {
                client.BaseAddress = new Uri($"http://{chOptions.Host}:{chOptions.Port}/");
                client.Timeout = TimeSpan.FromSeconds(60);
            })
            .AddStandardResilienceHandler();

            services.AddSingleton<IAiAnalyticsRepository, ClickHouseAiAnalyticsRepository>();

            services.AddHealthChecks()
                .AddCheck<ClickHouseAiHealthCheck>("ai-clickhouse-analytics", HealthStatus.Degraded, ["health", "ready"]);
        }
        else
        {
            services.AddSingleton<IAiAnalyticsRepository, NullAiAnalyticsRepository>();
        }

        // Elasticsearch removido — implementar busca semântica via pgvector ou PostgreSQL FTS
        services.AddSingleton<IAiSearchRepository, NullAiSearchRepository>();

        return services;
    }
}
