using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ApproveSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.ApproveSubscription.ApproveSubscription;
using CreateApiKeyFeature = NexTraceOne.Catalog.Application.Portal.Features.CreateApiKey.CreateApiKey;
using CreateSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.CreateSubscription.CreateSubscription;
using DeleteSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.DeleteSubscription.DeleteSubscription;
using GenerateCodeFeature = NexTraceOne.Catalog.Application.Portal.Features.GenerateCode.GenerateCode;
using GetApiConsumersFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiConsumers.GetApiConsumers;
using GetApiDetailFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiDetail.GetApiDetail;
using GetApiHealthFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiHealth.GetApiHealth;
using GetApiUsageAnalyticsFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApiUsageAnalytics.GetApiUsageAnalytics;
using GetApisIConsumeFeature = NexTraceOne.Catalog.Application.Portal.Features.GetApisIConsume.GetApisIConsume;
using GetAssetTimelineFeature = NexTraceOne.Catalog.Application.Portal.Features.GetAssetTimeline.GetAssetTimeline;
using GetMyApisFeature = NexTraceOne.Catalog.Application.Portal.Features.GetMyApis.GetMyApis;
using GetPlaygroundHistoryFeature = NexTraceOne.Catalog.Application.Portal.Features.GetPlaygroundHistory.GetPlaygroundHistory;
using GetPortalAnalyticsFeature = NexTraceOne.Catalog.Application.Portal.Features.GetPortalAnalytics.GetPortalAnalytics;
using GetRateLimitPolicyFeature = NexTraceOne.Catalog.Application.Portal.Features.GetRateLimitPolicy.GetRateLimitPolicy;
using GetSubscriptionsFeature = NexTraceOne.Catalog.Application.Portal.Features.GetSubscriptions.GetSubscriptions;
using ListApiKeysFeature = NexTraceOne.Catalog.Application.Portal.Features.ListApiKeys.ListApiKeys;
using RecordAnalyticsEventFeature = NexTraceOne.Catalog.Application.Portal.Features.RecordAnalyticsEvent.RecordAnalyticsEvent;
using RejectSubscriptionFeature = NexTraceOne.Catalog.Application.Portal.Features.RejectSubscription.RejectSubscription;
using RenderOpenApiContractFeature = NexTraceOne.Catalog.Application.Portal.Features.RenderOpenApiContract.RenderOpenApiContract;
using RevokeApiKeyFeature = NexTraceOne.Catalog.Application.Portal.Features.RevokeApiKey.RevokeApiKey;
using SearchCatalogFeature = NexTraceOne.Catalog.Application.Portal.Features.SearchCatalog.SearchCatalog;
using SetRateLimitPolicyFeature = NexTraceOne.Catalog.Application.Portal.Features.SetRateLimitPolicy.SetRateLimitPolicy;
using ValidateApiKeyFeature = NexTraceOne.Catalog.Application.Portal.Features.ValidateApiKey.ValidateApiKey;

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
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new SearchCatalogFeature.Query(searchTerm ?? string.Empty, typeFilter, statusFilter, ownerFilter, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/my-apis — APIs que o utilizador é dono
        group.MapGet("/catalog/my-apis", async (
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var ownerId = Guid.Parse(currentUser.Id);
            var result = await sender.Send(
                new GetMyApisFeature.Query(ownerId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // GET /api/v1/developerportal/catalog/consuming — APIs que o utilizador consome
        group.MapGet("/catalog/consuming", async (
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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

        // ── API Keys ──

        // POST /api/v1/developerportal/api-keys — Criar nova API Key
        group.MapPost("/api-keys", async (
            CreateApiKeyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // GET /api/v1/developerportal/api-keys — Listar API Keys do utilizador
        group.MapGet("/api-keys", async (
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var ownerId = Guid.Parse(currentUser.Id);
            var result = await sender.Send(
                new ListApiKeysFeature.Query(ownerId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // DELETE /api/v1/developerportal/api-keys/{apiKeyId} — Revogar API Key
        group.MapDelete("/api-keys/{apiKeyId:guid}", async (
            Guid apiKeyId,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var requesterId = Guid.Parse(currentUser.Id);
            var result = await sender.Send(
                new RevokeApiKeyFeature.Command(apiKeyId, requesterId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // POST /api/v1/developerportal/api-keys/validate — Validar API Key raw
        group.MapPost("/api-keys/validate", async (
            ValidateApiKeyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:write");

        // ── Subscription Approval ──

        // POST /api/v1/developerportal/subscriptions/{subscriptionId}/approve — Aprovar subscrição
        group.MapPost("/subscriptions/{subscriptionId:guid}/approve", async (
            Guid subscriptionId,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ApproveSubscriptionFeature.Command(subscriptionId, currentUser.Id),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:admin");

        // POST /api/v1/developerportal/subscriptions/{subscriptionId}/reject — Rejeitar subscrição
        group.MapPost("/subscriptions/{subscriptionId:guid}/reject", async (
            Guid subscriptionId,
            RejectSubscriptionRequestBody body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RejectSubscriptionFeature.Command(subscriptionId, body.RejectionReason),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:admin");

        // ── Usage Analytics ──

        // GET /api/v1/developerportal/analytics/usage — Métricas de uso de API
        group.MapGet("/analytics/usage", async (
            Guid? apiAssetId,
            Guid? consumerId,
            string? apiVersion,
            int daysBack,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetApiUsageAnalyticsFeature.Query(apiAssetId, consumerId, apiVersion, daysBack),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // ── Rate Limiting ──

        // PUT /api/v1/developerportal/catalog/{apiAssetId}/rate-limit — Definir política de rate limit
        group.MapPut("/catalog/{apiAssetId:guid}/rate-limit", async (
            Guid apiAssetId,
            SetRateLimitPolicyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { ApiAssetId = apiAssetId }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:admin");

        // GET /api/v1/developerportal/catalog/{apiAssetId}/rate-limit — Obter política de rate limit
        group.MapGet("/catalog/{apiAssetId:guid}/rate-limit", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetRateLimitPolicyFeature.Query(apiAssetId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");
    }

    private sealed record RejectSubscriptionRequestBody(string RejectionReason);
}
