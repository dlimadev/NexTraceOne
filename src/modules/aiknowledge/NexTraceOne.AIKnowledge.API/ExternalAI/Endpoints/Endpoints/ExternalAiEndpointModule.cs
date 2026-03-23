using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Enums;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using QueryExternalAISimpleFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple.QueryExternalAISimple;
using QueryExternalAIAdvancedFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced.QueryExternalAIAdvanced;
using CaptureExternalAIResponseFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.CaptureExternalAIResponse.CaptureExternalAIResponse;
using ApproveKnowledgeCaptureFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ApproveKnowledgeCapture.ApproveKnowledgeCapture;
using ListKnowledgeCapturesFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ListKnowledgeCaptures.ListKnowledgeCaptures;
using GetExternalAIUsageFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.GetExternalAIUsage.GetExternalAIUsage;
using ReuseKnowledgeCaptureFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ReuseKnowledgeCapture.ReuseKnowledgeCapture;
using ConfigureExternalAIPolicyFeature = NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ConfigureExternalAIPolicy.ConfigureExternalAIPolicy;

namespace NexTraceOne.AIKnowledge.API.ExternalAI.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo ExternalAi.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Política de autorização:
/// - Escrita: "ai:runtime:write" para endpoints de execução de queries e captura de conhecimento.
/// - Leitura: "ai:runtime:read" para listagem e métricas.
/// </summary>
public sealed class ExternalAiEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapQueryEndpoints(app);
        MapKnowledgeCaptureEndpoints(app);
    }

    private static void MapQueryEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/externalai/query").RequireRateLimiting("ai");

        group.MapPost("/simple", async (
            QueryExternalAISimpleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/advanced", async (
            QueryExternalAIAdvancedFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private static void MapKnowledgeCaptureEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/externalai/knowledge").RequireRateLimiting("ai");

        group.MapPost("/capture", async (
            CaptureExternalAIResponseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapGet("/captures", async (
            KnowledgeStatus? status,
            string? category,
            string? tags,
            string? textFilter,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListKnowledgeCapturesFeature.Query(status, category, tags, textFilter, from, to, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapPost("/captures/{captureId:guid}/approve", async (
            Guid captureId,
            ApproveKnowledgeCaptureRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ApproveKnowledgeCaptureFeature.Command(captureId, req.ReviewNotes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapPost("/captures/{captureId:guid}/reuse", async (
            Guid captureId,
            ReuseKnowledgeCaptureRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ReuseKnowledgeCaptureFeature.Command(captureId, req.NewContext, req.Purpose);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");

        group.MapGet("/usage", async (
            DateTimeOffset? from,
            DateTimeOffset? to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExternalAIUsageFeature.Query(from, to);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapPost("/policy", async (
            ConfigureExternalAIPolicyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    private sealed record ApproveKnowledgeCaptureRequest(string? ReviewNotes);
    private sealed record ReuseKnowledgeCaptureRequest(string NewContext, string? Purpose);
}
