using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
using GetAiUsageDashboardFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiUsageDashboard.GetAiUsageDashboard;
using ListSkillsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListSkills.ListSkills;
using GetSkillDetailsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetSkillDetails.GetSkillDetails;
using RegisterSkillFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterSkill.RegisterSkill;
using UpdateSkillFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateSkill.UpdateSkill;
using PublishSkillFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.PublishSkill.PublishSkill;
using DeprecateSkillFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.DeprecateSkill.DeprecateSkill;
using ExecuteSkillFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteSkill.ExecuteSkill;
using RateSkillExecutionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.RateSkillExecution.RateSkillExecution;
using SeedDefaultSkillsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SeedDefaultSkills.SeedDefaultSkills;

using SubmitAgentExecutionFeedbackFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAgentExecutionFeedback.SubmitAgentExecutionFeedback;
using GetAgentPerformanceDashboardFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPerformanceDashboard.GetAgentPerformanceDashboard;

using LoadSkillFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.LoadSkill.LoadSkill;
using CreateWarRoomFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateWarRoom.CreateWarRoom;
using GetWarRoomFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetWarRoom.GetWarRoom;
using ListWarRoomsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListWarRooms.ListWarRooms;
using ResolveWarRoomFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ResolveWarRoom.ResolveWarRoom;
using CalculateChangeConfidenceFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CalculateChangeConfidence.CalculateChangeConfidence;
using ProcessNaturalLanguageQueryFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ProcessNaturalLanguageQuery.ProcessNaturalLanguageQuery;
using AcknowledgeGuardianAlertFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.AcknowledgeGuardianAlert.AcknowledgeGuardianAlert;
using DismissGuardianAlertFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.DismissGuardianAlert.DismissGuardianAlert;
using ListGuardianAlertsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardianAlerts.ListGuardianAlerts;
using RecordMemoryNodeFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.RecordMemoryNode.RecordMemoryNode;
using QueryOrganizationalMemoryFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.QueryOrganizationalMemory.QueryOrganizationalMemory;
using GetMemoryNodeDetailsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetMemoryNodeDetails.GetMemoryNodeDetails;

using QuantifyTechDebtFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.QuantifyTechDebt.QuantifyTechDebt;
using GetSlaIntelligenceFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetSlaIntelligence.GetSlaIntelligence;
using ProposeSelfHealingActionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ProposeSelfHealingAction.ProposeSelfHealingAction;
using ApproveSelfHealingActionFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ApproveSelfHealingAction.ApproveSelfHealingAction;
using ListSelfHealingActionsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListSelfHealingActions.ListSelfHealingActions;

using CreateEvaluationSuiteFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationSuite.CreateEvaluationSuite;
using GetEvaluationSuiteFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluationSuite.GetEvaluationSuite;
using ListEvaluationSuitesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluationSuites.ListEvaluationSuites;
using CreateEvaluationRunFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationRun.CreateEvaluationRun;
using GetEvaluationRunFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluationRun.GetEvaluationRun;
using CreateEvaluationDatasetFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationDataset.CreateEvaluationDataset;

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
        MapUsageDashboardEndpoints(app);
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
        MapSkillsEndpoints(app);
        MapAgentLightningEndpoints(app);
        MapWarRoomEndpoints(app);
        MapChangeConfidenceEndpoints(app);
        MapNaturalLanguageQueryEndpoints(app);
        MapGuardianAlertEndpoints(app);
        MapOrganizationalMemoryEndpoints(app);
        MapAnalyticsEndpoints(app);
        MapSelfHealingEndpoints(app);
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
                body.ContextJson,
                body.TeamId);
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

    // ── Usage Dashboard ─────────────────────────────────────────────────

    private static void MapUsageDashboardEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/usage");

        group.MapGet("/dashboard", async (
            string? period,
            string? groupBy,
            int? top,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAiUsageDashboardFeature.Query(period, groupBy, top),
                cancellationToken);
            return result.ToHttpResult(localizer);
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

        group.MapPost("/skills", async (
            SeedDefaultSkillsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
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

    // ── Skills System (Phase 9) ─────────────────────────────────────────

    private static void MapSkillsEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/skills");

        group.MapGet("/", async (
            string? status,
            string? ownershipType,
            Guid? tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListSkillsFeature.Query(status, ownershipType, tenantId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:read");

        group.MapGet("/{skillId:guid}", async (
            Guid skillId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetSkillDetailsFeature.Query(skillId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:read");

        group.MapPost("/", async (
            RegisterSkillFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:write");

        group.MapPut("/{skillId:guid}", async (
            Guid skillId,
            UpdateSkillRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateSkillFeature.Command(
                skillId,
                body.DisplayName,
                body.Description,
                body.SkillContent,
                body.Tags,
                body.RequiredTools,
                body.PreferredModels,
                body.InputSchema,
                body.OutputSchema,
                body.IsComposable);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:write");

        group.MapPost("/{skillId:guid}/publish", async (
            Guid skillId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new PublishSkillFeature.Command(skillId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:write");

        group.MapPost("/{skillId:guid}/deprecate", async (
            Guid skillId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DeprecateSkillFeature.Command(skillId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:write");

        group.MapPost("/{skillId:guid}/execute", async (
            Guid skillId,
            ExecuteSkillRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ExecuteSkillFeature.Command(
                skillId,
                body.InputJson,
                body.ModelOverride,
                body.AgentId,
                body.ExecutedBy,
                TenantId: Guid.Empty);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:execute");

        group.MapPost("/executions/{executionId:guid}/feedback", async (
            Guid executionId,
            RateSkillExecutionRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new RateSkillExecutionFeature.Command(
                executionId,
                body.Rating,
                body.Outcome,
                body.Comment,
                body.ActualOutcome,
                body.WasCorrect,
                body.SubmittedBy,
                TenantId: Guid.Empty);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:execute");

        group.MapGet("/{skillName}/load", async (
            string skillName,
            Guid tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LoadSkillFeature.Query(skillName, tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:skills:read");
    }

    // ── Agent Lightning ─────────────────────────────────────────────────

    private static void MapAgentLightningEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/executions/{executionId:guid}/feedback", async (
            Guid executionId,
            SubmitFeedbackRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new SubmitAgentExecutionFeedbackFeature.Command(
                executionId, request.Rating, request.Outcome,
                request.Comment, request.ActualOutcome, request.WasCorrect,
                request.TimeToResolveMinutes, request.SubmittedBy, request.TenantId);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:agents:execute");

        app.MapGet("/api/v1/ai/agent-performance", async (
            Guid tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAgentPerformanceDashboardFeature.Query(tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");
    }

    // ── War Rooms (Phase 11) ─────────────────────────────────────────────

    private static void MapWarRoomEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/war-rooms");

        group.MapGet("/", async (
            Guid tenantId,
            string? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListWarRoomsFeature.Query(tenantId, status), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{warRoomId:guid}", async (
            Guid warRoomId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetWarRoomFeature.Query(warRoomId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/", async (
            CreateWarRoomFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/{warRoomId:guid}/resolve", async (
            Guid warRoomId,
            ResolveWarRoomRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ResolveWarRoomFeature.Command(warRoomId, body.PostMortemDraft), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Change Confidence (Phase 11) ─────────────────────────────────────

    private static void MapChangeConfidenceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/change-confidence");

        group.MapPost("/calculate", async (
            CalculateChangeConfidenceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Natural Language Query (Phase 11) ────────────────────────────────

    private static void MapNaturalLanguageQueryEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/nlq/process", async (
            ProcessNaturalLanguageQueryFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");
    }

    // ── Guardian Alerts (Phase 11) ───────────────────────────────────────

    private static void MapGuardianAlertEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/guardian-alerts");

        group.MapGet("/", async (
            Guid tenantId,
            string? serviceName,
            string? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListGuardianAlertsFeature.Query(tenantId, serviceName, status), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapPost("/{alertId:guid}/acknowledge", async (
            Guid alertId,
            AcknowledgeAlertRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new AcknowledgeGuardianAlertFeature.Command(alertId, body.AcknowledgedBy), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapPost("/{alertId:guid}/dismiss", async (
            Guid alertId,
            DismissAlertRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DismissGuardianAlertFeature.Command(alertId, body.Reason), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");
    }

    // ── Organizational Memory (Phase 11) ─────────────────────────────────

    private static void MapOrganizationalMemoryEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/memory");

        group.MapPost("/", async (
            RecordMemoryNodeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:write");

        group.MapGet("/search", async (
            string subject,
            Guid tenantId,
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new QueryOrganizationalMemoryFeature.Query(subject, tenantId, limit ?? 10), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");

        group.MapGet("/{nodeId:guid}", async (
            Guid nodeId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetMemoryNodeDetailsFeature.Query(nodeId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:governance:read");
    }

    private static void MapAnalyticsEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/tech-debt/quantify",
            async (QuantifyTechDebtRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var query = new QuantifyTechDebtFeature.Query(
                    req.ServiceName, req.TenantId, req.IncidentCountLast90Days,
                    req.TestCoveragePercent, req.CircularDependencies, req.AveragePrSizeLines,
                    req.AverageMttrMinutes, req.HourlyEngineeringRate);
                var result = await mediator.Send(query, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:governance:read")
            .WithTags("AI Analytics")
            .WithSummary("Quantify tech debt with financial impact");

        app.MapPost("/api/v1/ai/sla/intelligence",
            async (GetSlaIntelligenceRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var query = new GetSlaIntelligenceFeature.Query(
                    req.ServiceName, req.TenantId, req.CurrentSlaTarget,
                    req.ActualAvailabilityPercent, req.MaintenanceWindowMinutesPerMonth,
                    req.DeploymentFailuresLast12m, req.FridayDeployCount,
                    req.EstimatedPenaltyPerBreachMonth);
                var result = await mediator.Send(query, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:governance:read")
            .WithTags("AI Analytics")
            .WithSummary("Get SLA intelligence and breach analysis");
    }

    private static void MapSelfHealingEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/self-healing/actions",
            async (ProposeSelfHealingActionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var command = new ProposeSelfHealingActionFeature.Command(
                    req.IncidentId, req.ServiceName, req.ActionType,
                    req.ActionDescription, req.Confidence, req.RiskLevel, req.TenantId);
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:governance:write")
            .WithTags("AI Self-Healing")
            .WithSummary("Propose a self-healing remediation action");

        app.MapGet("/api/v1/ai/self-healing/actions",
            async (Guid tenantId, string? incidentId, bool pendingOnly, IMediator mediator, CancellationToken ct) =>
            {
                var query = new ListSelfHealingActionsFeature.Query(tenantId, incidentId, pendingOnly);
                var result = await mediator.Send(query, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:governance:read")
            .WithTags("AI Self-Healing")
            .WithSummary("List self-healing actions");

        app.MapPost("/api/v1/ai/self-healing/actions/{actionId:guid}/approve",
            async (Guid actionId, ApproveSelfHealingActionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var command = new ApproveSelfHealingActionFeature.Command(actionId, req.ApprovedBy);
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:governance:write")
            .WithTags("AI Self-Healing")
            .WithSummary("Approve a pending self-healing action");

        // ── ADR-009: AI Evaluation Harness ────────────────────────────────────

        var evalSuitesGroup = app.MapGroup("/api/v1/aiorchestration/evaluation/suites");

        evalSuitesGroup.MapPost("/",
            async (CreateEvaluationSuiteRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var command = new CreateEvaluationSuiteFeature.Command(
                    req.Name, req.DisplayName, req.Description ?? string.Empty,
                    req.UseCase, req.Version, req.TenantId, req.TargetModelId);
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:evaluation:author")
            .WithTags("AI Evaluation Harness")
            .WithSummary("Create evaluation suite");

        evalSuitesGroup.MapGet("/",
            async (Guid tenantId, string? useCase, int page, int pageSize, IMediator mediator, CancellationToken ct) =>
            {
                var query = new ListEvaluationSuitesFeature.Query(tenantId, useCase, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize);
                var result = await mediator.Send(query, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:evaluation:read")
            .WithTags("AI Evaluation Harness")
            .WithSummary("List evaluation suites");

        evalSuitesGroup.MapGet("/{suiteId:guid}",
            async (Guid suiteId, IMediator mediator, CancellationToken ct) =>
            {
                var query = new GetEvaluationSuiteFeature.Query(suiteId);
                var result = await mediator.Send(query, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:evaluation:read")
            .WithTags("AI Evaluation Harness")
            .WithSummary("Get evaluation suite");

        var evalRunsGroup = app.MapGroup("/api/v1/aiorchestration/evaluation/runs");

        evalRunsGroup.MapPost("/",
            async (CreateEvaluationRunRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var command = new CreateEvaluationRunFeature.Command(req.SuiteId, req.ModelId, req.PromptVersion, req.TenantId);
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:evaluation:author")
            .WithTags("AI Evaluation Harness")
            .WithSummary("Create evaluation run");

        evalRunsGroup.MapGet("/{runId:guid}",
            async (Guid runId, IMediator mediator, CancellationToken ct) =>
            {
                var query = new GetEvaluationRunFeature.Query(runId);
                var result = await mediator.Send(query, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:evaluation:read")
            .WithTags("AI Evaluation Harness")
            .WithSummary("Get evaluation run");

        var evalDatasetsGroup = app.MapGroup("/api/v1/aiorchestration/evaluation/datasets");

        evalDatasetsGroup.MapPost("/",
            async (CreateEvaluationDatasetRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var command = new CreateEvaluationDatasetFeature.Command(
                    req.Name, req.Description ?? string.Empty, req.UseCase, req.SourceType, req.TenantId);
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization("ai:evaluation:author")
            .WithTags("AI Evaluation Harness")
            .WithSummary("Create evaluation dataset");
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
    string? ContextJson,
    string? TeamId = null);

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

/// <summary>Corpo de pedido para atualização de uma skill de IA.</summary>
public sealed record UpdateSkillRequest(
    string DisplayName,
    string Description,
    string SkillContent,
    string[]? Tags,
    string[]? RequiredTools,
    string[]? PreferredModels,
    string? InputSchema,
    string? OutputSchema,
    bool? IsComposable);

/// <summary>Corpo de pedido para execução de uma skill de IA.</summary>
public sealed record ExecuteSkillRequest(
    string InputJson,
    string? ModelOverride,
    Guid? AgentId,
    string ExecutedBy);

/// <summary>Corpo de pedido para feedback de execução de skill.</summary>
public sealed record RateSkillExecutionRequest(
    int Rating,
    string Outcome,
    string? Comment,
    string? ActualOutcome,
    bool WasCorrect,
    string SubmittedBy);

/// <summary>Corpo de pedido para feedback de trajectória de agent (Agent Lightning).</summary>
public sealed record SubmitFeedbackRequest(
    int Rating,
    string Outcome,
    string? Comment,
    string? ActualOutcome,
    bool WasCorrect,
    int? TimeToResolveMinutes,
    string SubmittedBy,
    Guid TenantId);

/// <summary>Corpo de pedido para resolução de uma War Room.</summary>
public sealed record ResolveWarRoomRequest(string PostMortemDraft);

/// <summary>Corpo de pedido para reconhecimento de um alerta do Guardian.</summary>
public sealed record AcknowledgeAlertRequest(string AcknowledgedBy);

/// <summary>Corpo de pedido para descarte de um alerta do Guardian.</summary>
public sealed record DismissAlertRequest(string Reason);

/// <summary>Corpo de pedido para quantificação de dívida técnica.</summary>
public sealed record QuantifyTechDebtRequest(
    string ServiceName,
    Guid TenantId,
    int IncidentCountLast90Days,
    double TestCoveragePercent,
    int CircularDependencies,
    double AveragePrSizeLines,
    double AverageMttrMinutes,
    decimal HourlyEngineeringRate);

/// <summary>Corpo de pedido para inteligência de SLA.</summary>
public sealed record GetSlaIntelligenceRequest(
    string ServiceName,
    Guid TenantId,
    double CurrentSlaTarget,
    double ActualAvailabilityPercent,
    int MaintenanceWindowMinutesPerMonth,
    int DeploymentFailuresLast12m,
    int FridayDeployCount,
    decimal EstimatedPenaltyPerBreachMonth);

/// <summary>Corpo de pedido para proposta de acção de auto-remediação.</summary>
public sealed record ProposeSelfHealingActionRequest(
    string IncidentId,
    string ServiceName,
    string ActionType,
    string ActionDescription,
    double Confidence,
    string RiskLevel,
    Guid TenantId);

/// <summary>Corpo de pedido para aprovação de acção de auto-remediação.</summary>
public sealed record ApproveSelfHealingActionRequest(string ApprovedBy);

/// <summary>Corpo de pedido para criação de suite de avaliação.</summary>
public sealed record CreateEvaluationSuiteRequest(
    string Name,
    string DisplayName,
    string? Description,
    string UseCase,
    string Version,
    Guid TenantId,
    Guid? TargetModelId);

/// <summary>Corpo de pedido para criação de execução de avaliação.</summary>
public sealed record CreateEvaluationRunRequest(
    Guid SuiteId,
    Guid ModelId,
    string PromptVersion,
    Guid TenantId);

/// <summary>Corpo de pedido para criação de dataset de avaliação.</summary>
public sealed record CreateEvaluationDatasetRequest(
    string Name,
    string? Description,
    string UseCase,
    string SourceType,
    Guid TenantId);
