using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using RecordAnalyticsEventFeature = NexTraceOne.Governance.Application.Features.RecordAnalyticsEvent.RecordAnalyticsEvent;
using GetAnalyticsSummaryFeature = NexTraceOne.Governance.Application.Features.GetAnalyticsSummary.GetAnalyticsSummary;
using GetModuleAdoptionFeature = NexTraceOne.Governance.Application.Features.GetModuleAdoption.GetModuleAdoption;
using GetPersonaUsageFeature = NexTraceOne.Governance.Application.Features.GetPersonaUsage.GetPersonaUsage;
using GetJourneysFeature = NexTraceOne.Governance.Application.Features.GetJourneys.GetJourneys;
using GetValueMilestonesFeature = NexTraceOne.Governance.Application.Features.GetValueMilestones.GetValueMilestones;
using GetFrictionIndicatorsFeature = NexTraceOne.Governance.Application.Features.GetFrictionIndicators.GetFrictionIndicators;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Product Analytics — disponibiliza métricas de adoção, valor, fricção, jornadas e milestones.
/// Analytics orientados a decisão de produto, não a vanity metrics.
/// Privacy-aware: sem coleta excessiva de PII.
///
/// COMPATIBILIDADE TRANSITÓRIA (P2.4):
/// Este endpoint module está temporariamente alojado em Governance.API por compatibilidade de rota.
/// O ownership real pertence ao módulo Product Analytics.
/// Os handlers já consomem NexTraceOne.ProductAnalytics.Application.Abstractions e NexTraceOne.ProductAnalytics.Domain.
/// As permissões "governance:analytics:*" são residuais e serão renomeadas para "analytics:*" em fase futura.
/// A migração definitiva para ProductAnalytics.API está prevista para fase futura.
/// A rota /api/v1/product-analytics é do módulo Product Analytics, não de Governance.
/// </summary>
public sealed class ProductAnalyticsEndpointModule
{
    /// <summary>Registra endpoints de product analytics no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/product-analytics");

        // ────────────────────────────────────────
        // Evento de analytics
        // ────────────────────────────────────────

        group.MapPost("/events", async (
            RecordAnalyticsEventFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:write");

        // ────────────────────────────────────────
        // Resumo consolidado
        // ────────────────────────────────────────

        group.MapGet("/summary", async (
            string? persona,
            string? module,
            string? teamId,
            string? domainId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAnalyticsSummaryFeature.Query(persona, module, teamId, domainId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:read");

        // ────────────────────────────────────────
        // Adoção por módulo
        // ────────────────────────────────────────

        group.MapGet("/adoption/modules", async (
            string? persona,
            string? teamId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetModuleAdoptionFeature.Query(persona, teamId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:read");

        // ────────────────────────────────────────
        // Uso por persona
        // ────────────────────────────────────────

        group.MapGet("/adoption/personas", async (
            string? persona,
            string? teamId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPersonaUsageFeature.Query(persona, teamId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:read");

        // ────────────────────────────────────────
        // Jornadas e funis
        // ────────────────────────────────────────

        group.MapGet("/journeys", async (
            string? journeyId,
            string? persona,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetJourneysFeature.Query(journeyId, persona, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:read");

        // ────────────────────────────────────────
        // Marcos de valor
        // ────────────────────────────────────────

        group.MapGet("/value-milestones", async (
            string? persona,
            string? teamId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetValueMilestonesFeature.Query(persona, teamId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:read");

        // ────────────────────────────────────────
        // Indicadores de fricção
        // ────────────────────────────────────────

        group.MapGet("/friction", async (
            string? persona,
            string? module,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFrictionIndicatorsFeature.Query(persona, module, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:analytics:read");
    }
}
