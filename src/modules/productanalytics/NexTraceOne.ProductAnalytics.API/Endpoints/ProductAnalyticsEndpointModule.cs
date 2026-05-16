using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using RecordAnalyticsEventFeature = NexTraceOne.ProductAnalytics.Application.Features.RecordAnalyticsEvent.RecordAnalyticsEvent;
using GetAnalyticsSummaryFeature = NexTraceOne.ProductAnalytics.Application.Features.GetAnalyticsSummary.GetAnalyticsSummary;
using GetModuleAdoptionFeature = NexTraceOne.ProductAnalytics.Application.Features.GetModuleAdoption.GetModuleAdoption;
using GetPersonaUsageFeature = NexTraceOne.ProductAnalytics.Application.Features.GetPersonaUsage.GetPersonaUsage;
using GetJourneysFeature = NexTraceOne.ProductAnalytics.Application.Features.GetJourneys.GetJourneys;
using GetValueMilestonesFeature = NexTraceOne.ProductAnalytics.Application.Features.GetValueMilestones.GetValueMilestones;
using GetFrictionIndicatorsFeature = NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators.GetFrictionIndicators;
using GetAdoptionFunnelFeature = NexTraceOne.ProductAnalytics.Application.Features.GetAdoptionFunnel.GetAdoptionFunnel;
using GetFeatureHeatmapFeature = NexTraceOne.ProductAnalytics.Application.Features.GetFeatureHeatmap.GetFeatureHeatmap;
using ExportAnalyticsDataFeature = NexTraceOne.ProductAnalytics.Application.Features.ExportAnalyticsData.ExportAnalyticsData;
using GetCohortAnalysisFeature = NexTraceOne.ProductAnalytics.Application.Features.GetCohortAnalysis.GetCohortAnalysis;
using ListJourneyDefinitionsFeature = NexTraceOne.ProductAnalytics.Application.Features.ListJourneyDefinitions.ListJourneyDefinitions;
using CreateJourneyDefinitionFeature = NexTraceOne.ProductAnalytics.Application.Features.CreateJourneyDefinition.CreateJourneyDefinition;
using UpdateJourneyDefinitionFeature = NexTraceOne.ProductAnalytics.Application.Features.UpdateJourneyDefinition.UpdateJourneyDefinition;
using DeleteJourneyDefinitionFeature = NexTraceOne.ProductAnalytics.Application.Features.DeleteJourneyDefinition.DeleteJourneyDefinition;

namespace NexTraceOne.ProductAnalytics.API.Endpoints;

/// <summary>
/// Endpoints de Product Analytics — disponibiliza métricas de adoção, valor, fricção, jornadas e milestones.
/// Analytics orientados a decisão de produto, não a vanity metrics.
/// Privacy-aware: sem coleta excessiva de PII.
/// </summary>
public sealed class ProductAnalyticsEndpointModule
{
    /// <summary>Registra endpoints de product analytics no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/product-analytics")
            .WithTags("ProductAnalytics");

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
            if (result.IsFailure) return result.ToHttpResult(localizer);
            return Results.Created($"/api/v1/product-analytics/events/{result.Value.EventId}", result.Value);
        })
        .RequirePermission("analytics:write")
        .RequireRateLimiting("data-intensive")
        .WithSummary("Record analytics event")
        .WithDescription("Records a product analytics event triggered by user interaction in the frontend.");

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
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get analytics summary")
        .WithDescription("Returns consolidated metrics: total events, unique users, adoption score, value score, friction score, TTFV, TTCV and trend.");

        // ────────────────────────────────────────
        // Adoção por módulo
        // ────────────────────────────────────────

        group.MapGet("/adoption/modules", async (
            string? persona,
            string? teamId,
            string? range,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetModuleAdoptionFeature.Query(persona, teamId, range, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get module adoption metrics")
        .WithDescription("Returns adoption metrics per product module with pagination: adoption percent, depth score, unique users and top features. Supports ?page and ?pageSize.");

        // ────────────────────────────────────────
        // Uso por persona
        // ────────────────────────────────────────

        group.MapGet("/adoption/personas", async (
            string? persona,
            string? teamId,
            string? range,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPersonaUsageFeature.Query(persona, teamId, range, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get persona usage profiles")
        .WithDescription("Returns usage profiles per persona with pagination: active users, top modules, adoption depth, friction points and milestones reached. Supports ?page and ?pageSize.");

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
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get user journey analysis")
        .WithDescription("Returns journey completion rates, abandonment points and average step times. Returns skeleton journeys with 0% for tenants with no data.");

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
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get value milestones")
        .WithDescription("Returns product value milestones (TTFV, TTCV, first contract, first automation, etc.) with completion rates per user population.");

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
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get friction indicators")
        .WithDescription("Returns friction score and top friction points (zero result searches, empty states, abandoned journeys) by module and persona.");

        // ────────────────────────────────────────
        // Funil de adoção por módulo
        // ────────────────────────────────────────

        group.MapGet("/adoption/funnel", async (
            string? module,
            string? persona,
            string? teamId,
            string? range,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAdoptionFunnelFeature.Query(module, persona, teamId, range, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get adoption funnel by module")
        .WithDescription("Returns step-by-step adoption funnels per module with completion rates and biggest drop-off points. Supports ?page and ?pageSize.");

        // ────────────────────────────────────────
        // Mapa de calor de adoção de funcionalidades
        // ────────────────────────────────────────

        group.MapGet("/heatmap", async (
            string? persona,
            string? teamId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFeatureHeatmapFeature.Query(persona, teamId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get feature usage heatmap")
        .WithDescription("Returns feature-level usage heatmap: most and least used features per module with event counts.");

        // ────────────────────────────────────────
        // Exportação de dados (FEAT-04)
        // ────────────────────────────────────────

        group.MapGet("/export/events", async (
            string? persona,
            string? module,
            string? teamId,
            string? range,
            string? format,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var exportFormat = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
                ? ExportAnalyticsDataFeature.ExportFormat.Csv
                : ExportAnalyticsDataFeature.ExportFormat.Json;

            var query = new ExportAnalyticsDataFeature.Query(
                ExportAnalyticsDataFeature.ExportDataType.Events,
                exportFormat,
                persona, module, teamId, range);

            var result = await sender.Send(query, cancellationToken);
            if (!result.IsSuccess) return result.ToHttpResult(localizer);

            var r = result.Value;
            return Results.File(
                System.Text.Encoding.UTF8.GetBytes(r.Content),
                r.ContentType,
                r.FileName);
        })
        .RequirePermission("analytics:read")
        .RequireRateLimiting("data-intensive")
        .WithSummary("Export analytics events")
        .WithDescription("Exports raw session events in CSV or JSON format. Use ?format=csv or ?format=json. Max 10,000 rows; IsTruncated header indicates overflow.");

        group.MapGet("/export/summary", async (
            string? persona,
            string? module,
            string? teamId,
            string? range,
            string? format,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var exportFormat = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
                ? ExportAnalyticsDataFeature.ExportFormat.Csv
                : ExportAnalyticsDataFeature.ExportFormat.Json;

            var query = new ExportAnalyticsDataFeature.Query(
                ExportAnalyticsDataFeature.ExportDataType.Summary,
                exportFormat,
                persona, module, teamId, range);

            var result = await sender.Send(query, cancellationToken);
            if (!result.IsSuccess) return result.ToHttpResult(localizer);

            var r = result.Value;
            return Results.File(
                System.Text.Encoding.UTF8.GetBytes(r.Content),
                r.ContentType,
                r.FileName);
        })
        .RequirePermission("analytics:read")
        .RequireRateLimiting("data-intensive")
        .WithSummary("Export analytics summary")
        .WithDescription("Exports a consolidated analytics summary (total events, unique users, value/friction scores, top modules) in CSV or JSON format.");

        // ────────────────────────────────────────
        // Análise de cohorts (FEAT-05)
        // ────────────────────────────────────────

        group.MapGet("/cohorts", async (
            string? granularity,
            int? periods,
            string? metric,
            string? persona,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCohortAnalysisFeature.Query(granularity, periods, metric, persona, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:read")
        .WithSummary("Get cohort analysis")
        .WithDescription("Groups users by date of first event and computes retention/activation rates per cohort. Supports granularity=week|month, periods=1-24, metric=retention|activation.");

        // ────────────────────────────────────────
        // Configuração de jornadas (FEAT-03)
        // ────────────────────────────────────────

        var configGroup = group.MapGroup("/config/journeys")
            .WithTags("ProductAnalytics");

        configGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListJourneyDefinitionsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:read")
        .WithSummary("List journey definitions")
        .WithDescription("Returns active journey definitions for the current tenant, merged with global platform defaults.");

        configGroup.MapPost("/", async (
            CreateJourneyDefinitionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:configure")
        .WithSummary("Create journey definition")
        .WithDescription("Creates a new journey/funnel definition. Key must be unique per scope (tenant or global). Steps JSON: [{stepId, stepName, eventType}].");

        configGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateJourneyDefinitionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var effectiveCommand = command with { Id = id };
            var result = await sender.Send(effectiveCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:configure")
        .WithSummary("Update journey definition")
        .WithDescription("Updates the name, steps and active state of a journey definition. Key is immutable after creation.");

        configGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteJourneyDefinitionFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("analytics:configure")
        .WithSummary("Delete journey definition")
        .WithDescription("Permanently removes a tenant-owned journey definition. Global platform definitions cannot be deleted.");
    }
}
