using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.HandleModelFeedbackThresholdExceeded;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.AIKnowledge.Contracts.Governance.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Contracts.IntegrationEvents;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.EventHandlers;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance;

/// <summary>
/// Registra serviços de infraestrutura do módulo AiGovernance.
/// Inclui: DbContext, Repositórios, Adapters externos, Quartz Jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("AiGovernanceDatabase", "NexTraceOne");

        services.AddDbContext<AiGovernanceDbContext>((serviceProvider, options) =>
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

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AiGovernanceDbContext>());
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
        // Repository for persisted tool definitions (Phase 4)
        services.AddScoped<IAiToolDefinitionRepository, AiToolDefinitionRepository>();
        services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
        services.AddScoped<IAiGuardrailRepository, AiGuardrailRepository>();
        services.AddScoped<IAiEvaluationRepository, AiEvaluationRepository>();
        services.AddScoped<IAiFeedbackRepository, AiFeedbackRepository>();
        services.AddScoped<IOnboardingSessionRepository, OnboardingSessionRepository>();
        services.AddScoped<IIdeQuerySessionRepository, IdeQuerySessionRepository>();
        services.AddScoped<IAiModelAuthorizationService, AiModelAuthorizationService>();
        services.AddScoped<IAiExecutionPlanRepository, AiExecutionPlanRepository>();
        services.AddScoped<IAiAgentRuntimeService, AiAgentRuntimeService>();

        // ── Phase 9: Skills System ────────────────────────────────────────
        services.AddScoped<IAiSkillRepository, AiSkillRepository>();
        services.AddScoped<IAiSkillExecutionRepository, AiSkillExecutionRepository>();
        services.AddScoped<IAiSkillFeedbackRepository, AiSkillFeedbackRepository>();

        // ── Integration Event Handlers (E-M02) ───────────────────────────
        services.AddScoped<HandleModelFeedbackThresholdExceededHandler>();
        services.AddScoped<
            IIntegrationEventHandler<ModelFeedbackThresholdExceededIntegrationEvent>,
            ModelFeedbackThresholdExceededEventHandlerAdapter>();

        // Cross-module contract — consumed by AiOrchestration for token/model attribution
        services.AddScoped<IAiGovernanceModule, AiGovernanceModuleService>();

        // Background jobs
        services.AddHostedService<FeedbackThresholdJob>();
        services.AddHostedService<AiDataRetentionJob>();
        services.AddHostedService<EmbeddingIndexJob>();

        return services;
    }
}
