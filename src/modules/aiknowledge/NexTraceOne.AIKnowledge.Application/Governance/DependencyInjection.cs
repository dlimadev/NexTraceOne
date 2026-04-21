using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Governance.Features.CreateConversation;
using NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePolicy;
using NexTraceOne.AIKnowledge.Application.Governance.Features.EnrichContext;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgent;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentExecution;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetConversation;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluation;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetGuardrail;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeCapabilities;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetModel;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetPromptTemplate;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetRoutingDecision;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetToolDefinition;
using NexTraceOne.AIKnowledge.Application.Governance.Features.HandleModelFeedbackThresholdExceeded;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgentsByContext;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListAuditEntries;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListBudgets;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListConversations;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluations;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardrails;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeCapabilityPolicies;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeClients;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListKnowledgeSourceWeights;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListMessages;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListModels;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListPolicies;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListPromptTemplates;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListSuggestedPrompts;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ListToolDefinitions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.PlanExecution;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterIdeClient;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterModel;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateBudget;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateConversation;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateModel;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetExternalDataSource;
using NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterExternalDataSource;
using NexTraceOne.AIKnowledge.Application.Governance.Features.SyncExternalDataSource;
using NexTraceOne.AIKnowledge.Application.Governance.Features.ToggleExternalDataSource;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateExternalDataSource;
using NexTraceOne.AIKnowledge.Application.Governance.Features.UpdatePolicy;
using NexTraceOne.AIKnowledge.Application.Governance.Services;
using NexTraceOne.BuildingBlocks.Application;

namespace NexTraceOne.AIKnowledge.Application.Governance;

/// <summary>
/// Registra serviços da camada Application do módulo AiGovernance.
/// Inclui: MediatR handlers, FluentValidation validators para todas as features
/// de governança de IA — Model Registry, Access Policies, Budgets, Audit,
/// Assistant conversations e suggested prompts.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAiGovernanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Model Registry ───────────────────────────────────────────────
        services.AddTransient<IValidator<RegisterModel.Command>, RegisterModel.Validator>();
        services.AddTransient<IValidator<UpdateModel.Command>, UpdateModel.Validator>();

        // ── Access Policies ──────────────────────────────────────────────
        services.AddTransient<IValidator<CreatePolicy.Command>, CreatePolicy.Validator>();
        services.AddTransient<IValidator<UpdatePolicy.Command>, UpdatePolicy.Validator>();

        // ── Budgets ──────────────────────────────────────────────────────
        services.AddTransient<IValidator<UpdateBudget.Command>, UpdateBudget.Validator>();

        // ── AI Assistant ─────────────────────────────────────────────────
        services.AddTransient<IValidator<SendAssistantMessage.Command>, SendAssistantMessage.Validator>();
        services.AddTransient<IValidator<CreateConversation.Command>, CreateConversation.Validator>();
        services.AddTransient<IValidator<UpdateConversation.Command>, UpdateConversation.Validator>();

        // ── AI Governance services (extracted from SendAssistantMessage) ──
        services.AddScoped<IContextGroundingService, ContextGroundingService>();
        services.AddScoped<IAiRoutingResolver, AiRoutingResolver>();
        services.AddScoped<IConversationPersistenceService, ConversationPersistenceService>();
        services.AddScoped<IAiGuardrailEnforcementService, AiGuardrailEnforcementService>();

        // ── IDE Integrations ─────────────────────────────────────────────
        services.AddTransient<IValidator<RegisterIdeClient.Command>, RegisterIdeClient.Validator>();

        // ── AI Routing & Enrichment ──────────────────────────────────────
        services.AddTransient<IValidator<PlanExecution.Command>, PlanExecution.Validator>();
        services.AddTransient<IValidator<EnrichContext.Command>, EnrichContext.Validator>();

        // ── Query Validators ─────────────────────────────────────────────
        services.AddTransient<IValidator<GetAgent.Query>, GetAgent.Validator>();
        services.AddTransient<IValidator<GetAgentExecution.Query>, GetAgentExecution.Validator>();
        services.AddTransient<IValidator<GetConversation.Query>, GetConversation.Validator>();
        services.AddTransient<IValidator<GetEvaluation.Query>, GetEvaluation.Validator>();
        services.AddTransient<IValidator<GetGuardrail.Query>, GetGuardrail.Validator>();
        services.AddTransient<IValidator<GetIdeCapabilities.Query>, GetIdeCapabilities.Validator>();
        services.AddTransient<IValidator<GetModel.Query>, GetModel.Validator>();
        services.AddTransient<IValidator<GetPromptTemplate.Query>, GetPromptTemplate.Validator>();
        services.AddTransient<IValidator<GetRoutingDecision.Query>, GetRoutingDecision.Validator>();
        services.AddTransient<IValidator<GetToolDefinition.Query>, GetToolDefinition.Validator>();
        services.AddTransient<IValidator<ListAgentsByContext.Query>, ListAgentsByContext.Validator>();
        services.AddTransient<IValidator<ListAuditEntries.Query>, ListAuditEntries.Validator>();
        services.AddTransient<IValidator<ListBudgets.Query>, ListBudgets.Validator>();
        services.AddTransient<IValidator<ListConversations.Query>, ListConversations.Validator>();
        services.AddTransient<IValidator<ListEvaluations.Query>, ListEvaluations.Validator>();
        services.AddTransient<IValidator<ListGuardrails.Query>, ListGuardrails.Validator>();
        services.AddTransient<IValidator<ListIdeCapabilityPolicies.Query>, ListIdeCapabilityPolicies.Validator>();
        services.AddTransient<IValidator<ListIdeClients.Query>, ListIdeClients.Validator>();
        services.AddTransient<IValidator<ListKnowledgeSourceWeights.Query>, ListKnowledgeSourceWeights.Validator>();
        services.AddTransient<IValidator<ListMessages.Query>, ListMessages.Validator>();
        services.AddTransient<IValidator<ListModels.Query>, ListModels.Validator>();
        services.AddTransient<IValidator<ListPolicies.Query>, ListPolicies.Validator>();
        services.AddTransient<IValidator<ListPromptTemplates.Query>, ListPromptTemplates.Validator>();
        services.AddTransient<IValidator<ListSuggestedPrompts.Query>, ListSuggestedPrompts.Validator>();
        services.AddTransient<IValidator<ListToolDefinitions.Query>, ListToolDefinitions.Validator>();

        // ── External Data Sources (Extensible RAG) ────────────────────────
        services.AddTransient<IValidator<RegisterExternalDataSource.Command>, RegisterExternalDataSource.Validator>();
        services.AddTransient<IValidator<UpdateExternalDataSource.Command>, UpdateExternalDataSource.Validator>();
        services.AddTransient<IValidator<ToggleExternalDataSource.Command>, ToggleExternalDataSource.Validator>();
        services.AddTransient<IValidator<SyncExternalDataSource.Command>, SyncExternalDataSource.Validator>();
        services.AddTransient<IValidator<GetExternalDataSource.Query>, GetExternalDataSource.Validator>();

        return services;
    }
}
