using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using CloneDashboardFeature = NexTraceOne.Governance.Application.Features.CloneDashboard.CloneDashboard;
using CreateCustomDashboardFeature = NexTraceOne.Governance.Application.Features.CreateCustomDashboard.CreateCustomDashboard;
using DeleteCustomDashboardFeature = NexTraceOne.Governance.Application.Features.DeleteCustomDashboard.DeleteCustomDashboard;
using GetCustomDashboardFeature = NexTraceOne.Governance.Application.Features.GetCustomDashboard.GetCustomDashboard;
using GetDashboardRenderDataFeature = NexTraceOne.Governance.Application.Features.GetDashboardRenderData.GetDashboardRenderData;
using GetDashboardHistoryFeature = NexTraceOne.Governance.Application.Features.GetDashboardHistory.GetDashboardHistory;
using ListCustomDashboardsFeature = NexTraceOne.Governance.Application.Features.ListCustomDashboards.ListCustomDashboards;
using UpdateCustomDashboardFeature = NexTraceOne.Governance.Application.Features.UpdateCustomDashboard.UpdateCustomDashboard;
using RevertDashboardFeature = NexTraceOne.Governance.Application.Features.RevertDashboard.RevertDashboard;
using ShareDashboardFeature = NexTraceOne.Governance.Application.Features.ShareDashboard.ShareDashboard;
using RecordTechnicalDebtFeature = NexTraceOne.Governance.Application.Features.RecordTechnicalDebt.RecordTechnicalDebt;
using GetTechnicalDebtSummaryFeature = NexTraceOne.Governance.Application.Features.GetTechnicalDebtSummary.GetTechnicalDebtSummary;
using ExecuteNqlQueryFeature = NexTraceOne.Governance.Application.Features.ExecuteNqlQuery.ExecuteNqlQuery;
using ValidateNqlQueryFeature = NexTraceOne.Governance.Application.Features.ValidateNqlQuery.ValidateNqlQuery;
using GetDashboardAnnotationsFeature = NexTraceOne.Governance.Application.Features.GetDashboardAnnotations.GetDashboardAnnotations;
using GetDashboardLiveStreamFeature = NexTraceOne.Governance.Application.Features.GetDashboardLiveStream.GetDashboardLiveStream;
using GetWidgetDeltaFeature = NexTraceOne.Governance.Application.Features.GetWidgetDelta.GetWidgetDelta;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Custom Dashboards e Technical Debt do módulo Governance.
/// Disponibiliza builder de dashboards por persona e tracking de dívida técnica.
/// </summary>
public sealed class DashboardsAndDebtEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        MapDashboardEndpoints(app);
        MapTechnicalDebtEndpoints(app);
        MapNqlEndpoints(app);
        MapLiveEndpoints(app);
    }

    private static void MapDashboardEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/dashboards");

        // ── Criar dashboard customizado ──
        group.MapPost("/", async (
            CreateCustomDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // ── Listar dashboards customizados ──
        group.MapGet("/", async (
            string tenantId,
            string? persona,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListCustomDashboardsFeature.Query(tenantId, persona, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Detalhe de dashboard customizado ──
        group.MapGet("/{dashboardId:guid}", async (
            Guid dashboardId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCustomDashboardFeature.Query(dashboardId, tenantId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Render data de dashboard (estrutura para o frontend renderizar) ──
        group.MapGet("/{dashboardId:guid}/render-data", async (
            Guid dashboardId,
            string tenantId,
            string? environmentId,
            string? timeRange,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardRenderDataFeature.Query(
                dashboardId, tenantId, environmentId, timeRange);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Atualizar dashboard customizado ──
        group.MapPut("/{dashboardId:guid}", async (
            Guid dashboardId,
            UpdateCustomDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = dashboardId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // ── Eliminar dashboard customizado ──
        group.MapDelete("/{dashboardId:guid}", async (
            Guid dashboardId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = new DeleteCustomDashboardFeature.Command(dashboardId, tenantId);
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // ── Clonar dashboard ──
        group.MapPost("/{dashboardId:guid}/clone", async (
            Guid dashboardId,
            CloneDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { SourceDashboardId = dashboardId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/governance/dashboards/{r.CloneId}", localizer);
        }).RequirePermission("governance:reports:write");

        // ── Histórico de revisões do dashboard (V3.1) ──
        group.MapGet("/{dashboardId:guid}/history", async (
            Guid dashboardId,
            string tenantId,
            int maxResults,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardHistoryFeature.Query(dashboardId, tenantId, maxResults);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Reverter dashboard para revisão anterior (V3.1) ──
        group.MapPost("/{dashboardId:guid}/revert", async (
            Guid dashboardId,
            RevertDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = dashboardId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // ── Definir política de partilha granular (V3.1) ──
        group.MapPost("/{dashboardId:guid}/share", async (
            Guid dashboardId,
            ShareDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = dashboardId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");
    }

    private static void MapNqlEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/nql");

        // ── Executar query NQL (Wave V3.2) ──
        group.MapPost("/execute", async (
            ExecuteNqlQueryFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Validar query NQL sem executar (Wave V3.2) ──
        group.MapPost("/validate", async (
            ValidateNqlQueryFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Anotações de dashboard por intervalo temporal (Wave V3.2) ──
        var dashGroup = app.MapGroup("/api/v1/governance/dashboards");
        dashGroup.MapGet("/annotations", async (
            string tenantId,
            DateTimeOffset from,
            DateTimeOffset to,
            string? services,
            int maxPerSource,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var serviceList = string.IsNullOrWhiteSpace(services)
                ? null
                : (IReadOnlyList<string>)services.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var query = new GetDashboardAnnotationsFeature.Query(tenantId, from, to, serviceList, maxPerSource);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }

    private static void MapLiveEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/dashboards");

        // ── SSE live stream para um dashboard (Wave V3.3) ──
        group.MapGet("/{dashboardId:guid}/live", async (
            Guid dashboardId,
            string tenantId,
            string? widgetIds,
            HttpContext ctx,
            CancellationToken cancellationToken) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection   = "keep-alive";

            var widgetList = string.IsNullOrWhiteSpace(widgetIds)
                ? null
                : (IReadOnlyList<string>)widgetIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var query = new GetDashboardLiveStreamFeature.Query(dashboardId, tenantId, widgetList);

            await foreach (var evt in GetDashboardLiveStreamFeature.GenerateEventsAsync(query, cancellationToken))
            {
                var frame = GetDashboardLiveStreamFeature.ToSseFrame(evt);
                await ctx.Response.WriteAsync(frame, cancellationToken);
                await ctx.Response.Body.FlushAsync(cancellationToken);
            }
        }).RequirePermission("governance:reports:read");

        // ── Delta de widget desde timestamp (Wave V3.3) ──
        group.MapGet("/{dashboardId:guid}/widgets/{widgetId}/delta", async (
            Guid dashboardId,
            string widgetId,
            string tenantId,
            DateTimeOffset since,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetWidgetDeltaFeature.Query(dashboardId, widgetId, tenantId, since);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }

    private static void MapTechnicalDebtEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/technical-debt");

        // ── Registar item de dívida técnica ──
        group.MapPost("/", async (
            RecordTechnicalDebtFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // ── Resumo de dívida técnica ──
        group.MapGet("/summary", async (
            string? serviceName,
            string? teamName,
            int topN,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTechnicalDebtSummaryFeature.Query(serviceName, teamName, topN);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }
}
