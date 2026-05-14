using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ExecuteAiChatFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteAiChat.ExecuteAiChat;
using ListAiProvidersFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiProviders.ListAiProviders;
using CheckAiProvidersHealthFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.CheckAiProvidersHealth.CheckAiProvidersHealth;
using ListAiSourcesFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiSources.ListAiSources;
using SearchDocumentsFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.SearchDocuments.SearchDocuments;
using SearchDataFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.SearchData.SearchData;
using SearchTelemetryFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.SearchTelemetry.SearchTelemetry;
using ListAiModelsFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiModels.ListAiModels;
using ActivateModelFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ActivateModel.ActivateModel;
using GetTokenUsageFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.GetTokenUsage.GetTokenUsage;
using ListTokenPoliciesFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ListTokenPolicies.ListTokenPolicies;
using RecordExternalInferenceFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.RecordExternalInference.RecordExternalInference;

namespace NexTraceOne.AIKnowledge.API.Runtime.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AI Runtime.
/// Fornece acesso à inferência de chat, listagem de providers e verificação de saúde
/// com governança integrada.
///
/// Política de autorização:
/// - Leitura: "ai:runtime:read" para endpoints de consulta de providers e saúde.
/// - Escrita: "ai:runtime:write" para endpoints de execução de inferência.
/// </summary>
public sealed class AiRuntimeEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapChatEndpoints(app);
        MapProviderEndpoints(app);
        MapSearchEndpoints(app);
        MapSourceEndpoints(app);
        MapModelEndpoints(app);
        MapTokenEndpoints(app);
        MapExternalInferenceEndpoints(app);
    }

    // ── Chat ─────────────────────────────────────────────────────────────

    private static void MapChatEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai").RequireRateLimiting("ai");

        group.MapPost("/chat", async (
            ExecuteAiChatRequest body,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ExecuteAiChatFeature.Command(
                body.ConversationId,
                body.Message,
                body.PreferredModelId,
                body.SystemPrompt,
                body.Temperature,
                body.MaxTokens);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/chat/stream", async (
            ExecuteAiChatStreamRequest body,
            [FromServices] IAiProviderFactory providerFactory,
            [FromServices] IAiModelCatalogService modelCatalogService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // 1. Resolve model
            ResolvedModel? resolvedModel;
            if (body.PreferredModelId.HasValue)
            {
                resolvedModel = await modelCatalogService.ResolveModelByIdAsync(
                    body.PreferredModelId.Value, cancellationToken);
            }
            else
            {
                resolvedModel = await modelCatalogService.ResolveDefaultModelAsync(
                    "chat", cancellationToken);
            }

            if (resolvedModel is null)
            {
                httpContext.Response.StatusCode = 404;
                await httpContext.Response.WriteAsJsonAsync(
                    new { code = "AI.ModelNotFound", messageKey = "ai.model.not_found" },
                    cancellationToken);
                return;
            }

            // 2. Get chat provider
            var chatProvider = providerFactory.GetChatProvider(resolvedModel.ProviderId);
            if (chatProvider is null)
            {
                httpContext.Response.StatusCode = 404;
                await httpContext.Response.WriteAsJsonAsync(
                    new { code = "AI.ProviderNotFound", messageKey = "ai.provider.not_found",
                          @params = new[] { resolvedModel.ProviderId } },
                    cancellationToken);
                return;
            }

            // 3. Build chat request
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(body.SystemPrompt))
                messages.Add(new ChatMessage("system", body.SystemPrompt));
            messages.Add(new ChatMessage("user", body.Message));

            var chatRequest = new ChatCompletionRequest(
                resolvedModel.ModelName,
                messages,
                body.Temperature,
                body.MaxTokens,
                body.SystemPrompt);

            // 4. Write SSE streaming response
            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            await foreach (var chunk in chatProvider.CompleteStreamingAsync(chatRequest, cancellationToken))
            {
                var sseData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    content = chunk.Content,
                    isComplete = chunk.IsComplete,
                    modelId = chunk.ModelId,
                    providerId = chunk.ProviderId,
                    promptTokens = chunk.PromptTokens,
                    completionTokens = chunk.CompletionTokens,
                    error = chunk.ErrorMessage
                });

                await httpContext.Response.WriteAsync($"data: {sseData}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);

                if (chunk.IsComplete)
                    break;
            }

            await httpContext.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }).RequirePermission("ai:runtime:write");
    }

    // ── Providers ────────────────────────────────────────────────────────

    private static void MapProviderEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/providers");

        group.MapGet("/", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAiProvidersFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapGet("/health", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CheckAiProvidersHealthFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");
    }

    // ── Search ───────────────────────────────────────────────────────────

    private static void MapSearchEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/search");

        group.MapPost("/documents", async (
            SearchDocumentsRequest body,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new SearchDocumentsFeature.Command(
                body.Query,
                body.MaxResults,
                body.SourceFilter,
                body.ClassificationFilter);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/data", async (
            SearchDataRequest body,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new SearchDataFeature.Command(
                body.Query,
                body.EntityType,
                body.TenantId,
                body.MaxResults);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/telemetry", async (
            SearchTelemetryRequest body,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new SearchTelemetryFeature.Command(
                body.Query,
                body.TraceId,
                body.SpanId,
                body.ServiceName,
                body.Severity,
                body.From,
                body.To,
                body.MaxResults);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    // ── Sources ──────────────────────────────────────────────────────────

    private static void MapSourceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/sources");

        group.MapGet("/", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAiSourcesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapGet("/health", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAiSourcesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");
    }

    // ── Models ───────────────────────────────────────────────────────────

    private static void MapModelEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/models");

        group.MapGet("/active", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAiModelsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapPut("/{id:guid}/activate", async (
            Guid id,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ActivateModelFeature.Command(id);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    // ── Token governance ─────────────────────────────────────────────────

    private static void MapTokenEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai");

        group.MapGet("/token-policies", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListTokenPoliciesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapGet("/token-usage", async (
            string? userId,
            Guid? tenantId,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTokenUsageFeature.Query(userId, tenantId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");
    }

    // ── External inferences ──────────────────────────────────────────────

    private static void MapExternalInferenceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai");

        group.MapPost("/external-inferences", async (
            RecordExternalInferenceRequest body,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new RecordExternalInferenceFeature.Command(
                body.ProviderId,
                body.ModelName,
                body.OriginalPrompt,
                body.AdditionalContext,
                body.ResponseContent,
                body.SensitivityClassification,
                body.QualityScore,
                body.CanPromoteToSharedMemory);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    // ── Request DTOs ─────────────────────────────────────────────────────

    internal sealed record ExecuteAiChatRequest(
        string Message,
        Guid? ConversationId,
        Guid? PreferredModelId,
        string? SystemPrompt,
        double? Temperature,
        int? MaxTokens);

    internal sealed record ExecuteAiChatStreamRequest(
        string Message,
        Guid? PreferredModelId,
        string? SystemPrompt,
        double? Temperature,
        int? MaxTokens);

    internal sealed record SearchDocumentsRequest(
        string Query,
        int? MaxResults,
        string? SourceFilter,
        string? ClassificationFilter);

    internal sealed record SearchDataRequest(
        string Query,
        string? EntityType,
        string? TenantId,
        int? MaxResults);

    internal sealed record SearchTelemetryRequest(
        string Query,
        string? TraceId,
        string? SpanId,
        string? ServiceName,
        string? Severity,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int? MaxResults);

    internal sealed record RecordExternalInferenceRequest(
        string ProviderId,
        string ModelName,
        string OriginalPrompt,
        string? AdditionalContext,
        string ResponseContent,
        string SensitivityClassification,
        int? QualityScore,
        bool CanPromoteToSharedMemory);
}
