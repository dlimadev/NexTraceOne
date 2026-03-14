using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.DeveloperPortal.Domain.Enums;
using CreateSubscriptionFeature = NexTraceOne.DeveloperPortal.Application.Features.CreateSubscription.CreateSubscription;
using DeleteSubscriptionFeature = NexTraceOne.DeveloperPortal.Application.Features.DeleteSubscription.DeleteSubscription;
using ExecutePlaygroundFeature = NexTraceOne.DeveloperPortal.Application.Features.ExecutePlayground.ExecutePlayground;
using GenerateCodeFeature = NexTraceOne.DeveloperPortal.Application.Features.GenerateCode.GenerateCode;
using GetApiConsumersFeature = NexTraceOne.DeveloperPortal.Application.Features.GetApiConsumers.GetApiConsumers;
using GetApiDetailFeature = NexTraceOne.DeveloperPortal.Application.Features.GetApiDetail.GetApiDetail;
using GetApiHealthFeature = NexTraceOne.DeveloperPortal.Application.Features.GetApiHealth.GetApiHealth;
using GetApisIConsumeFeature = NexTraceOne.DeveloperPortal.Application.Features.GetApisIConsume.GetApisIConsume;
using GetAssetTimelineFeature = NexTraceOne.DeveloperPortal.Application.Features.GetAssetTimeline.GetAssetTimeline;
using GetMyApisFeature = NexTraceOne.DeveloperPortal.Application.Features.GetMyApis.GetMyApis;
using GetPlaygroundHistoryFeature = NexTraceOne.DeveloperPortal.Application.Features.GetPlaygroundHistory.GetPlaygroundHistory;
using GetPortalAnalyticsFeature = NexTraceOne.DeveloperPortal.Application.Features.GetPortalAnalytics.GetPortalAnalytics;
using GetSubscriptionsFeature = NexTraceOne.DeveloperPortal.Application.Features.GetSubscriptions.GetSubscriptions;
using RecordAnalyticsEventFeature = NexTraceOne.DeveloperPortal.Application.Features.RecordAnalyticsEvent.RecordAnalyticsEvent;
using RenderOpenApiContractFeature = NexTraceOne.DeveloperPortal.Application.Features.RenderOpenApiContract.RenderOpenApiContract;
using SearchCatalogFeature = NexTraceOne.DeveloperPortal.Application.Features.SearchCatalog.SearchCatalog;

namespace NexTraceOne.DeveloperPortal.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo DeveloperPortal.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Endpoints organizados por funcionalidade: catálogo, subscrições, playground, geração de código e analytics.
///
/// Política de autorização:
/// - Endpoints de leitura do catálogo exigem "developer-portal:read".
/// - Endpoints de escrita (subscrições, playground, codegen, analytics) exigem "developer-portal:write".
/// </summary>
public sealed class DeveloperPortalEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/developerportal");

        // ── Catálogo ──

        // GET /api/v1/developerportal/catalog/search — Pesquisa no catálogo de APIs
        group.MapGet("/catalog/search", async (
            string? searchTerm,
            string? typeFilter,
            string? statusFilter,
            string? ownerFilter,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SearchCatalogFeature.Query(searchTerm ?? string.Empty, typeFilter, statusFilter, ownerFilter, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/my-apis — APIs que o utilizador é dono
        group.MapGet("/catalog/my-apis", async (
            Guid ownerId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetMyApisFeature.Query(ownerId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/consuming — APIs que o utilizador consome
        group.MapGet("/catalog/consuming", async (
            Guid userId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetApisIConsumeFeature.Query(userId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/{apiAssetId} — Detalhe de uma API
        group.MapGet("/catalog/{apiAssetId:guid}", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetApiDetailFeature.Query(apiAssetId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/{apiAssetId}/health — Saúde de uma API
        group.MapGet("/catalog/{apiAssetId:guid}/health", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetApiHealthFeature.Query(apiAssetId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/{apiAssetId}/timeline — Timeline de uma API
        group.MapGet("/catalog/{apiAssetId:guid}/timeline", async (
            Guid apiAssetId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetAssetTimelineFeature.Query(apiAssetId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/{apiAssetId}/consumers — Consumidores de uma API
        group.MapGet("/catalog/{apiAssetId:guid}/consumers", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetApiConsumersFeature.Query(apiAssetId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/{apiAssetId}/contract — Contrato OpenAPI renderizado
        group.MapGet("/catalog/{apiAssetId:guid}/contract", async (
            Guid apiAssetId,
            string? version,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RenderOpenApiContractFeature.Query(apiAssetId, version),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // ── Subscrições ──

        // POST /api/v1/developerportal/subscriptions — Criar subscrição
        group.MapPost("/subscriptions", async (
            CreateSubscriptionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // GET /api/v1/developerportal/subscriptions — Listar subscrições do utilizador
        group.MapGet("/subscriptions", async (
            Guid subscriberId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetSubscriptionsFeature.Query(subscriberId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // DELETE /api/v1/developerportal/subscriptions/{subscriptionId} — Remover subscrição
        group.MapDelete("/subscriptions/{subscriptionId:guid}", async (
            Guid subscriptionId,
            Guid requesterId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DeleteSubscriptionFeature.Command(subscriptionId, requesterId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // ── Playground ──

        // POST /api/v1/developerportal/playground/execute — Executar chamada no playground
        group.MapPost("/playground/execute", async (
            ExecutePlaygroundFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // GET /api/v1/developerportal/playground/history — Histórico de sessões do playground
        group.MapGet("/playground/history", async (
            Guid userId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetPlaygroundHistoryFeature.Query(userId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // ── Geração de Código ──

        // POST /api/v1/developerportal/codegen — Gerar código a partir de contrato
        group.MapPost("/codegen", async (
            GenerateCodeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // ── Analytics ──

        // POST /api/v1/developerportal/analytics/events — Registar evento de analytics
        group.MapPost("/analytics/events", async (
            RecordAnalyticsEventFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // GET /api/v1/developerportal/analytics — Obter métricas de analytics
        group.MapGet("/analytics", async (
            int daysBack,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetPortalAnalyticsFeature.Query(daysBack),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");
    }
}
