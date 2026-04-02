using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ListModelsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListModels.ListModels;
using GetModelFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetModel.GetModel;
using RegisterModelFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterModel.RegisterModel;
using UpdateModelFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateModel.UpdateModel;
using ListPoliciesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListPolicies.ListPolicies;
using CreatePolicyFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePolicy.CreatePolicy;
using UpdatePolicyFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdatePolicy.UpdatePolicy;
using ListBudgetsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListBudgets.ListBudgets;
using UpdateBudgetFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateBudget.UpdateBudget;
using ListAuditEntriesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListAuditEntries.ListAuditEntries;
using ListKnowledgeSourcesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListKnowledgeSources.ListKnowledgeSources;
using SendAssistantMessageFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SendAssistantMessage.SendAssistantMessage;
using ListConversationsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListConversations.ListConversations;
using CreateConversationFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateConversation.CreateConversation;
using GetConversationFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetConversation.GetConversation;
using UpdateConversationFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateConversation.UpdateConversation;
using ListMessagesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListMessages.ListMessages;
using ListSuggestedPromptsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListSuggestedPrompts.ListSuggestedPrompts;
using ListRoutingStrategiesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListRoutingStrategies.ListRoutingStrategies;
using GetRoutingDecisionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetRoutingDecision.GetRoutingDecision;
using PlanExecutionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.PlanExecution.PlanExecution;
using ListKnowledgeSourceWeightsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListKnowledgeSourceWeights.ListKnowledgeSourceWeights;
using EnrichContextFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.EnrichContext.EnrichContext;
using ListAvailableModelsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListAvailableModels.ListAvailableModels;
using ListAgentsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgents.ListAgents;
using GetAgentFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgent.GetAgent;
using ListAgentsByContextFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListAgentsByContext.ListAgentsByContext;
using CreateAgentFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateAgent.CreateAgent;
using UpdateAgentFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateAgent.UpdateAgent;
using ExecuteAgentFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteAgent.ExecuteAgent;
using GetAgentExecutionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentExecution.GetAgentExecution;
using ReviewArtifactFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ReviewArtifact.ReviewArtifact;
using SeedDefaultModelsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultModels.SeedDefaultModels;
using SeedDefaultAgentsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultAgents.SeedDefaultAgents;
using SeedDefaultGuardrailsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultGuardrails.SeedDefaultGuardrails;
using SeedDefaultPromptTemplatesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultPromptTemplates.SeedDefaultPromptTemplates;
using SeedDefaultToolDefinitionsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultToolDefinitions.SeedDefaultToolDefinitions;
using ListGuardrailsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardrails.ListGuardrails;
using GetGuardrailFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetGuardrail.GetGuardrail;
using CreateGuardrailFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateGuardrail.CreateGuardrail;
using UpdateGuardrailFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateGuardrail.UpdateGuardrail;
using ListPromptTemplatesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListPromptTemplates.ListPromptTemplates;
using GetPromptTemplateFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetPromptTemplate.GetPromptTemplate;
using CreatePromptTemplateFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreatePromptTemplate.CreatePromptTemplate;
using UpdatePromptTemplateFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdatePromptTemplate.UpdatePromptTemplate;
using ListToolDefinitionsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListToolDefinitions.ListToolDefinitions;
using GetToolDefinitionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetToolDefinition.GetToolDefinition;
using CreateToolDefinitionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateToolDefinition.CreateToolDefinition;
using UpdateToolDefinitionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateToolDefinition.UpdateToolDefinition;
using ListEvaluationsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluations.ListEvaluations;
using GetEvaluationFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluation.GetEvaluation;
using SubmitEvaluationFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitEvaluation.SubmitEvaluation;

namespace NexTraceOne.AIKnowledge.API.Governance.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AI Governance.
/// Fornece acesso ao Model Registry, Access Policies, Budgets, Audit,
/// Knowledge Sources, AI Assistant, AI Routing e Knowledge Enrichment
/// com governança integrada.
///
/// Política de autorização:
/// - Leitura: "ai:governance:read" para endpoints de consulta.
/// - Escrita: "ai:governance:write" para endpoints de criação e atualização.
/// - Assistente leitura: "ai:assistant:read" para listagem de conversas e mensagens.
/// - Assistente escrita: "ai:assistant:write" para envio de mensagens e criação de conversas.
/// </summary>
public sealed class AiGovernanceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapModelRegistryEndpoints(app);
        MapAvailableModelsEndpoints(app);
        MapAccessPolicyEndpoints(app);
        MapBudgetEndpoints(app);
        MapAuditEndpoints(app);
        MapKnowledgeSourceEndpoints(app);
        MapAssistantEndpoints(app);
        MapRoutingEndpoints(app);
        MapEnrichmentEndpoints(app);
        MapAgentEndpoints(app);
        MapAgentExecutionEndpoints(app);
        MapAgentArtifactEndpoints(app);
        MapGuardrailEndpoints(app);
        MapPromptTemplateEndpoints(app);
        MapToolDefinitionEndpoints(app);
        MapEvaluationEndpoints(app);
        MapSeedEndpoints(app);
    }

    // ── Model Registry ──────────────────────────────────────────────────

    private static void MapModelRegistryEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/models");

        group.MapGet("/", async (
            string? provider,
            string? modelType,
            string? status,
            bool? isInternal,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var parsedModelType = modelType is not null
                ? Enum.Parse<ModelType>(modelType, ignoreCase: true)
                : (ModelType?)null;

            var parsedStatus = status is not null
                ? Enum.Parse<ModelStatus>(status, ignoreCase: true)
                : (ModelStatus?)null;

            var result = await sender.Send(
                new ListModelsFeature.Query(provider, parsedModelType, parsedStatus, isInternal),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{modelId:guid}", async (
            Guid modelId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetModelFeature.Query(modelId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            RegisterModelFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPatch("/{modelId:guid}", async (
            Guid modelId,
            UpdateModelRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateModelFeature.Command(
                modelId,
                body.DisplayName,
                body.Capabilities,
                body.DefaultUseCases,
                body.SensitivityLevel,
                body.NewStatus);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Available Models (per-user authorization) ────────────────────────

    private static void MapAvailableModelsEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/models");

        group.MapGet("/available", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAvailableModelsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");
    }

    // ── AI Agents ───────────────────────────────────────────────────────

    private static void MapAgentEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/agents");

        group.MapGet("/", async (
            bool? isOfficial,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAgentsFeature.Query(isOfficial), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        group.MapGet("/categories", () =>
            Results.Ok(new
            {
                items = typeof(AgentCategory).GetEnumNames(),
            }))
            .RequirePermission("ai:assistant:read");

        group.MapGet("/by-context", async (
            string context,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAgentsByContextFeature.Query(context), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        group.MapGet("/{agentId:guid}", async (
            Guid agentId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAgentFeature.Query(agentId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        group.MapPost("/", async (
            CreateAgentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPut("/{agentId:guid}", async (
            Guid agentId,
            UpdateAgentRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateAgentFeature.Command(
                agentId,
                body.DisplayName,
                body.Description,
                body.SystemPrompt,
                body.Objective,
                body.Capabilities,
                body.TargetPersona,
                body.Icon,
                body.PreferredModelId,
                body.AllowedModelIds,
                body.AllowedTools,
                body.InputSchema,
                body.OutputSchema,
                body.Visibility,
                body.AllowModelOverride,
                body.SortOrder);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/{agentId:guid}/execute", async (
            Guid agentId,
            ExecuteAgentRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ExecuteAgentFeature.Command(
                agentId,
                body.Input,
                body.ModelIdOverride,
                body.ContextJson);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");
    }

    // ── Agent Executions ────────────────────────────────────────────────

    private static void MapAgentExecutionEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/agent-executions");

        group.MapGet("/{executionId:guid}", async (
            Guid executionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAgentExecutionFeature.Query(executionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");
    }

    // ── Agent Artifacts ─────────────────────────────────────────────────

    private static void MapAgentArtifactEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/artifacts");

        group.MapPost("/{artifactId:guid}/review", async (
            Guid artifactId,
            ReviewArtifactRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ReviewArtifactFeature.Command(
                artifactId,
                body.Decision,
                body.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Access Policies ─────────────────────────────────────────────────

    private static void MapAccessPolicyEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/policies");

        group.MapGet("/", async (
            string? scope,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListPoliciesFeature.Query(scope, isActive), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            CreatePolicyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPatch("/{policyId:guid}", async (
            Guid policyId,
            UpdatePolicyRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePolicyFeature.Command(
                policyId,
                body.Description,
                body.AllowExternalAI,
                body.InternalOnly,
                body.MaxTokensPerRequest,
                body.EnvironmentRestrictions);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Budgets ─────────────────────────────────────────────────────────

    private static void MapBudgetEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/budgets");

        group.MapGet("/", async (
            string? scope,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListBudgetsFeature.Query(scope, isActive), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPatch("/{budgetId:guid}", async (
            Guid budgetId,
            UpdateBudgetRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateBudgetFeature.Command(
                budgetId,
                body.MaxTokens,
                body.MaxRequests,
                body.Period);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Audit ───────────────────────────────────────────────────────────

    private static void MapAuditEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/audit");

        group.MapGet("/", async (
            string? userId,
            Guid? modelId,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            string? result,
            string? clientType,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var parsedResult = result is not null
                ? Enum.Parse<UsageResult>(result, ignoreCase: true)
                : (UsageResult?)null;

            var parsedClientType = clientType is not null
                ? Enum.Parse<AIClientType>(clientType, ignoreCase: true)
                : (AIClientType?)null;

            var queryResult = await sender.Send(
                new ListAuditEntriesFeature.Query(
                    userId, modelId, startDate, endDate,
                    parsedResult, parsedClientType, pageSize ?? 50),
                cancellationToken);
            return queryResult.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");
    }

    // ── Knowledge Sources ───────────────────────────────────────────────

    private static void MapKnowledgeSourceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/knowledge-sources");

        group.MapGet("/", async (
            string? sourceType,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var parsedSourceType = sourceType is not null
                ? Enum.Parse<KnowledgeSourceType>(sourceType, ignoreCase: true)
                : (KnowledgeSourceType?)null;

            var result = await sender.Send(
                new ListKnowledgeSourcesFeature.Query(parsedSourceType, isActive),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/weights", async (
            string? useCaseType,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListKnowledgeSourceWeightsFeature.Query(useCaseType),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");
    }

    // ── AI Assistant (Mature) ───────────────────────────────────────────

    private static void MapAssistantEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/assistant");

        // ── Chat — envio de mensagem com contexto completo ──────────────
        group.MapPost("/chat", async (
            SendAssistantMessageFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");

        // ── Conversations — CRUD de conversas ───────────────────────────
        group.MapGet("/conversations", async (
            string? userId,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListConversationsFeature.Query(userId, pageSize ?? 20),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        group.MapGet("/conversations/{conversationId:guid}", async (
            Guid conversationId,
            int? messagePageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetConversationFeature.Query(conversationId, messagePageSize ?? 50),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        group.MapPost("/conversations", async (
            CreateConversationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");

        group.MapPatch("/conversations/{conversationId:guid}", async (
            Guid conversationId,
            UpdateConversationRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateConversationFeature.Command(
                conversationId,
                body.Title,
                body.Tags,
                body.Archive);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");

        // ── Messages — listagem de mensagens de uma conversa ────────────
        group.MapGet("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListMessagesFeature.Query(conversationId, pageSize ?? 50),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        // ── Suggested Prompts — sugestões contextuais por persona ───────
        group.MapGet("/prompts", async (
            string? persona,
            string? category,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListSuggestedPromptsFeature.Query(persona, category),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:read");

        // ── Plan Execution — planeamento de execução com roteamento ─────
        group.MapPost("/plan-execution", async (
            PlanExecutionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");
    }

    // ── AI Routing ──────────────────────────────────────────────────────

    private static void MapRoutingEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/routing");

        group.MapGet("/strategies", async (
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListRoutingStrategiesFeature.Query(isActive),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/decisions/{decisionId:guid}", async (
            Guid decisionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetRoutingDecisionFeature.Query(decisionId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");
    }

    // ── Knowledge Enrichment ────────────────────────────────────────────

    private static void MapEnrichmentEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/context");

        group.MapPost("/enrich", async (
            EnrichContextFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");
    }

    // ── Seed Defaults ──────────────────────────────────────────────────

    private static void MapSeedEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/seed");

        group.MapPost("/models", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SeedDefaultModelsFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/agents", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SeedDefaultAgentsFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/guardrails", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SeedDefaultGuardrailsFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/prompt-templates", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SeedDefaultPromptTemplatesFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/tool-definitions", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SeedDefaultToolDefinitionsFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Guardrails ──────────────────────────────────────────────────────

    private static void MapGuardrailEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/guardrails");

        group.MapGet("/", async (
            string? category,
            string? guardType,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListGuardrailsFeature.Query(category, guardType, isActive),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{guardrailId:guid}", async (
            Guid guardrailId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetGuardrailFeature.Query(guardrailId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            CreateGuardrailFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPatch("/{guardrailId:guid}", async (
            Guid guardrailId,
            UpdateGuardrailRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateGuardrailFeature.Command(guardrailId, body.IsActive);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Prompt Templates ────────────────────────────────────────────────

    private static void MapPromptTemplateEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/prompt-templates");

        group.MapGet("/", async (
            string? category,
            string? persona,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListPromptTemplatesFeature.Query(category, persona, isActive),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{templateId:guid}", async (
            Guid templateId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetPromptTemplateFeature.Query(templateId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            CreatePromptTemplateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPatch("/{templateId:guid}", async (
            Guid templateId,
            UpdatePromptTemplateRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePromptTemplateFeature.Command(templateId, body.IsActive);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Tool Definitions ────────────────────────────────────────────────

    private static void MapToolDefinitionEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/tool-definitions");

        group.MapGet("/", async (
            string? category,
            bool? isActive,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListToolDefinitionsFeature.Query(category, isActive),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{toolId:guid}", async (
            Guid toolId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetToolDefinitionFeature.Query(toolId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            CreateToolDefinitionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPatch("/{toolId:guid}", async (
            Guid toolId,
            UpdateToolDefinitionRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateToolDefinitionFeature.Command(toolId, body.IsActive);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Evaluations ─────────────────────────────────────────────────────

    private static void MapEvaluationEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/evaluations");

        group.MapGet("/", async (
            Guid? conversationId,
            Guid? agentExecutionId,
            string? userId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListEvaluationsFeature.Query(conversationId, agentExecutionId, userId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{evaluationId:guid}", async (
            Guid evaluationId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetEvaluationFeature.Query(evaluationId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            SubmitEvaluationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }
}

// ── Request DTOs para endpoints PATCH ───────────────────────────────────

/// <summary>Corpo de pedido para atualização de um modelo de IA.</summary>
public sealed record UpdateModelRequest(
    string? DisplayName,
    string? Capabilities,
    string? DefaultUseCases,
    int? SensitivityLevel,
    string? NewStatus);

/// <summary>Corpo de pedido para atualização de uma política de acesso de IA.</summary>
public sealed record UpdatePolicyRequest(
    string Description,
    bool AllowExternalAI,
    bool InternalOnly,
    int MaxTokensPerRequest,
    string? EnvironmentRestrictions);

/// <summary>Corpo de pedido para atualização de um budget de IA.</summary>
public sealed record UpdateBudgetRequest(
    long? MaxTokens,
    int? MaxRequests,
    string? Period);

/// <summary>Corpo de pedido para atualização de uma conversa do assistente de IA.</summary>
public sealed record UpdateConversationRequest(
    string? Title,
    string? Tags,
    bool? Archive);

/// <summary>Corpo de pedido para atualização de um agent customizado.</summary>
public sealed record UpdateAgentRequest(
    string DisplayName,
    string Description,
    string? SystemPrompt,
    string? Objective,
    string? Capabilities,
    string? TargetPersona,
    string? Icon,
    Guid? PreferredModelId,
    string? AllowedModelIds,
    string? AllowedTools,
    string? InputSchema,
    string? OutputSchema,
    string? Visibility,
    bool? AllowModelOverride,
    int? SortOrder);

/// <summary>Corpo de pedido para execução de um agent.</summary>
public sealed record ExecuteAgentRequest(
    string Input,
    Guid? ModelIdOverride,
    string? ContextJson);

/// <summary>Corpo de pedido para review de um artefacto.</summary>
public sealed record ReviewArtifactRequest(
    string Decision,
    string? Notes);

/// <summary>Corpo de pedido para atualização de um guardrail de IA.</summary>
public sealed record UpdateGuardrailRequest(bool? IsActive);

/// <summary>Corpo de pedido para atualização de um template de prompt.</summary>
public sealed record UpdatePromptTemplateRequest(bool? IsActive);

/// <summary>Corpo de pedido para atualização de uma definição de ferramenta.</summary>
public sealed record UpdateToolDefinitionRequest(bool? IsActive);
