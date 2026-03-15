using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListModelsFeature = NexTraceOne.AiGovernance.Application.Features.ListModels.ListModels;
using GetModelFeature = NexTraceOne.AiGovernance.Application.Features.GetModel.GetModel;
using RegisterModelFeature = NexTraceOne.AiGovernance.Application.Features.RegisterModel.RegisterModel;
using UpdateModelFeature = NexTraceOne.AiGovernance.Application.Features.UpdateModel.UpdateModel;
using ListPoliciesFeature = NexTraceOne.AiGovernance.Application.Features.ListPolicies.ListPolicies;
using CreatePolicyFeature = NexTraceOne.AiGovernance.Application.Features.CreatePolicy.CreatePolicy;
using UpdatePolicyFeature = NexTraceOne.AiGovernance.Application.Features.UpdatePolicy.UpdatePolicy;
using ListBudgetsFeature = NexTraceOne.AiGovernance.Application.Features.ListBudgets.ListBudgets;
using UpdateBudgetFeature = NexTraceOne.AiGovernance.Application.Features.UpdateBudget.UpdateBudget;
using ListAuditEntriesFeature = NexTraceOne.AiGovernance.Application.Features.ListAuditEntries.ListAuditEntries;
using ListKnowledgeSourcesFeature = NexTraceOne.AiGovernance.Application.Features.ListKnowledgeSources.ListKnowledgeSources;
using SendAssistantMessageFeature = NexTraceOne.AiGovernance.Application.Features.SendAssistantMessage.SendAssistantMessage;
using ListConversationsFeature = NexTraceOne.AiGovernance.Application.Features.ListConversations.ListConversations;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AI Governance.
/// Fornece acesso ao Model Registry, Access Policies, Budgets, Audit,
/// Knowledge Sources e AI Assistant com governança integrada.
///
/// Política de autorização:
/// - Leitura: "ai:governance:read" para endpoints de consulta.
/// - Escrita: "ai:governance:write" para endpoints de criação e atualização.
/// - Assistente leitura: "ai:assistant:read" para listagem de conversas.
/// - Assistente escrita: "ai:assistant:write" para envio de mensagens.
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
    }

    // ── AI Assistant ────────────────────────────────────────────────────

    private static void MapAssistantEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/assistant");

        group.MapPost("/chat", async (
            SendAssistantMessageFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:assistant:write");

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
