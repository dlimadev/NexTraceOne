using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.CreateSubscription.CreateSubscription;
using DeleteSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.DeleteSubscription.DeleteSubscription;
using GenerateCodeFeature = NexTraceOne.Catalog.Application.Portal.Features.GenerateCode.GenerateCode;
using GetApiConsumersFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiConsumers.GetApiConsumers;
using GetApiDetailFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiDetail.GetApiDetail;
using GetApiHealthFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiHealth.GetApiHealth;
using GetApisIConsumeFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApisIConsume.GetApisIConsume;
using GetAssetTimelineFeature = NexTraceOne.Catalog.Application.Portal.Features.GetAssetTimeline.GetAssetTimeline;
using GetMyApisFeature = NexTraceOne.Catalog.Application.Portal.Features.GetMyApis.GetMyApis;
using GetPlaygroundHistoryFeature = NexTraceOne.Catalog.Application.Portal.Features.GetPlaygroundHistory.GetPlaygroundHistory;
using GetPortalAnalyticsFeature = NexTraceOne.Catalog.Application.Portal.Features.GetPortalAnalytics.GetPortalAnalytics;
using GetSubscriptionsFeature = NexTraceOne.Catalog.Application.Portal.Features.GetSubscriptions.GetSubscriptions;
using RecordAnalyticsEventFeature = NexTraceOne.Catalog.Application.Portal.Features.RecordAnalyticsEvent.RecordAnalyticsEvent;
using RenderOpenApiContractFeature = NexTraceOne.Catalog.Application.Portal.Features.RenderOpenApiContract.RenderOpenApiContract;
using SearchCatalogFeature = NexTraceOne.Catalog.Application.Portal.Features.SearchCatalog.SearchCatalog;

namespace NexTraceOne.Catalog.API.Portal.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo DeveloperPortal.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Endpoints organizados por funcionalidade: catálogo, subscrições, histórico de consumo, geração de código e analytics.
///
/// Política de autorização:
/// - Endpoints de leitura do catálogo exigem "developer-portal:read".
/// - Endpoints de escrita (subscrições, codegen, analytics) exigem "developer-portal:write".
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
            int page,
            int pageSize,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var ownerId = Guid.Parse(currentUser.Id);
            var result = await sender.Send(
                new GetMyApisFeature.Query(ownerId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/consuming — APIs que o utilizador consome
        group.MapGet("/catalog/consuming", async (
            int page,
            int pageSize,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(currentUser.Id);
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
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var subscriberId = Guid.Parse(currentUser.Id);
            var result = await sender.Send(
                new GetSubscriptionsFeature.Query(subscriberId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // DELETE /api/v1/developerportal/subscriptions/{subscriptionId} — Remover subscrição
        group.MapDelete("/subscriptions/{subscriptionId:guid}", async (
            Guid subscriptionId,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var requesterId = Guid.Parse(currentUser.Id);
            var result = await sender.Send(
                new DeleteSubscriptionFeature.Command(subscriptionId, requesterId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // ── Histórico de playground ──

        // GET /api/v1/developerportal/playground/history — Histórico de sessões do playground
        group.MapGet("/playground/history", async (
            int page,
            int pageSize,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var userId = Guid.Parse(currentUser.Id);
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
