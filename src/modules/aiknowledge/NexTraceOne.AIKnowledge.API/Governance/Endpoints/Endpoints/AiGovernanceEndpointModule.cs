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
        MapAccessPolicyEndpoints(app);
        MapBudgetEndpoints(app);
        MapAuditEndpoints(app);
        MapKnowledgeSourceEndpoints(app);
        MapAssistantEndpoints(app);
        MapRoutingEndpoints(app);
        MapEnrichmentEndpoints(app);
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
