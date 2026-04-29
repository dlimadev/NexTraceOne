using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Governance.Application.Abstractions;
using IDashboardDataBridge = NexTraceOne.Governance.Application.Abstractions.IDashboardDataBridge;
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
using CreateNotebookFeature = NexTraceOne.Governance.Application.Features.CreateNotebook.CreateNotebook;
using GetNotebookFeature = NexTraceOne.Governance.Application.Features.GetNotebook.GetNotebook;
using ListNotebooksFeature = NexTraceOne.Governance.Application.Features.ListNotebooks.ListNotebooks;
using UpdateNotebookFeature = NexTraceOne.Governance.Application.Features.UpdateNotebook.UpdateNotebook;
using DeleteNotebookFeature = NexTraceOne.Governance.Application.Features.DeleteNotebook.DeleteNotebook;
using ComposeAiDashboardFeature = NexTraceOne.Governance.Application.Features.ComposeAiDashboard.ComposeAiDashboard;
using ScheduleDashboardReportFeature = NexTraceOne.Governance.Application.Features.ScheduleDashboardReport.ScheduleDashboardReport;
using ExportDashboardAsYamlFeature = NexTraceOne.Governance.Application.Features.ExportDashboardAsYaml.ExportDashboardAsYaml;
using DeprecateDashboardFeature = NexTraceOne.Governance.Application.Features.DeprecateDashboard.DeprecateDashboard;
using PublishDashboardFeature = NexTraceOne.Governance.Application.Features.PublishDashboard.PublishDashboard;
using RecordDashboardUsageFeature = NexTraceOne.Governance.Application.Features.RecordDashboardUsage.RecordDashboardUsage;
using GetDashboardUsageAnalyticsFeature = NexTraceOne.Governance.Application.Features.GetDashboardUsageAnalytics.GetDashboardUsageAnalytics;
using JoinPresenceSessionFeature = NexTraceOne.Governance.Application.Features.JoinPresenceSession.JoinPresenceSession;
using GetPresenceSessionsFeature = NexTraceOne.Governance.Application.Features.GetPresenceSessions.GetPresenceSessions;
using AddDashboardCommentFeature = NexTraceOne.Governance.Application.Features.AddDashboardComment.AddDashboardComment;
using GetDashboardCommentsFeature = NexTraceOne.Governance.Application.Features.GetDashboardComments.GetDashboardComments;
using ResolveCommentFeature = NexTraceOne.Governance.Application.Features.ResolveComment.ResolveComment;
using CreateDashboardMonitorFeature = NexTraceOne.Governance.Application.Features.CreateDashboardMonitor.CreateDashboardMonitor;
using ListDashboardMonitorsFeature = NexTraceOne.Governance.Application.Features.ListDashboardMonitors.ListDashboardMonitors;
using ListDashboardTemplatesFeature = NexTraceOne.Governance.Application.Features.ListDashboardTemplates.ListDashboardTemplates;
using InstantiateTemplateFeature = NexTraceOne.Governance.Application.Features.InstantiateTemplate.InstantiateTemplate;
using GetPersonaHomeFeature = NexTraceOne.Governance.Application.Features.GetPersonaHome.GetPersonaHome;
using ListScheduledDashboardReportsFeature = NexTraceOne.Governance.Application.Features.ListScheduledDashboardReports.ListScheduledDashboardReports;

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
        MapNotebookEndpoints(app);
        MapAiComposerEndpoints(app);
        MapReportsAndEmbedEndpoints(app);
        MapCollaborationEndpoints(app);
        MapMonitorEndpoints(app);
        MapTemplatesEndpoints(app);
        MapPersonaHomeEndpoints(app);
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
            IDashboardDataBridge bridge,
            CancellationToken cancellationToken) =>
        {
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection   = "keep-alive";

            var widgetList = string.IsNullOrWhiteSpace(widgetIds)
                ? null
                : (IReadOnlyList<string>)widgetIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var query = new GetDashboardLiveStreamFeature.Query(dashboardId, tenantId, widgetList);

            await foreach (var evt in GetDashboardLiveStreamFeature.GenerateEventsAsync(query, bridge, cancellationToken))
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

    // ── Wave V3.4 — Notebooks ────────────────────────────────────────────────

    private static void MapNotebookEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/notebooks");

        // POST /api/v1/governance/notebooks
        group.MapPost("/", async (
            CreateNotebookFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/governance/notebooks/{r.NotebookId}", localizer);
        }).RequirePermission("governance:reports:write");

        // GET /api/v1/governance/notebooks
        group.MapGet("/", async (
            string tenantId,
            string? persona,
            string? status,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListNotebooksFeature.Query(tenantId, persona, status, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // GET /api/v1/governance/notebooks/{notebookId}
        group.MapGet("/{notebookId:guid}", async (
            Guid notebookId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNotebookFeature.Query(notebookId, tenantId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // PUT /api/v1/governance/notebooks/{notebookId}
        group.MapPut("/{notebookId:guid}", async (
            Guid notebookId,
            UpdateNotebookFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { NotebookId = notebookId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // DELETE /api/v1/governance/notebooks/{notebookId}
        group.MapDelete("/{notebookId:guid}", async (
            Guid notebookId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = new DeleteNotebookFeature.Command(notebookId, tenantId);
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");
    }

    // ── Wave V3.4 — AI Dashboard Composer ────────────────────────────────────

    private static void MapAiComposerEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/ai");

        // POST /api/v1/governance/ai/compose-dashboard
        group.MapPost("/compose-dashboard", async (
            ComposeAiDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");
    }

    // ── Wave V3.6 — Governance, Reports &amp; Embedding ──────────────────────────

    private static void MapReportsAndEmbedEndpoints(IEndpointRouteBuilder app)
    {
        var dashboards = app.MapGroup("/api/v1/governance/dashboards");

        // GET /api/v1/governance/dashboards/scheduled-reports
        dashboards.MapGet("/scheduled-reports", async (
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListScheduledDashboardReportsFeature.Query(tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // POST /api/v1/governance/dashboards/{id}/schedule-report
        dashboards.MapPost("/{id:guid}/schedule-report", async (
            Guid id,
            ScheduleDashboardReportFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = id };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // GET /api/v1/governance/dashboards/{id}/export-yaml
        dashboards.MapGet("/{id:guid}/export-yaml", async (
            Guid id,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ExportDashboardAsYamlFeature.Query(id, tenantId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // POST /api/v1/governance/dashboards/{id}/publish
        dashboards.MapPost("/{id:guid}/publish", async (
            Guid id,
            PublishDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = id };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // POST /api/v1/governance/dashboards/{id}/deprecate
        dashboards.MapPost("/{id:guid}/deprecate", async (
            Guid id,
            DeprecateDashboardFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = id };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        // POST /api/v1/governance/dashboards/{id}/record-usage
        dashboards.MapPost("/{id:guid}/record-usage", async (
            Guid id,
            RecordDashboardUsageFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DashboardId = id };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // GET /api/v1/governance/dashboards/usage-analytics
        dashboards.MapGet("/usage-analytics", async (
            string tenantId,
            Guid? dashboardId,
            int windowDays,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardUsageAnalyticsFeature.Query(tenantId, dashboardId, windowDays > 0 ? windowDays : 30);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }

    // ── V3.7 — Real-time Collaboration ─────────────────────────────────────────
    private static void MapCollaborationEndpoints(IEndpointRouteBuilder app)
    {
        var collab = app.MapGroup("/api/v1/governance/collaboration");

        collab.MapPost("/presence", async (
            JoinPresenceSessionFeature.Command command,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        collab.MapGet("/presence", async (
            string resourceType, Guid resourceId, string tenantId,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPresenceSessionsFeature.Query(resourceType, resourceId, tenantId), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        collab.MapPost("/comments", async (
            AddDashboardCommentFeature.Command command,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        collab.MapGet("/comments", async (
            Guid dashboardId, string tenantId, string? widgetId, bool includeResolved,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDashboardCommentsFeature.Query(dashboardId, tenantId, widgetId, includeResolved), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        collab.MapPost("/comments/{id:guid}/resolve", async (
            Guid id, string tenantId, string userId,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResolveCommentFeature.Command(id, tenantId, userId), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");
    }

    // ── V3.9 — Alerting from Widget ────────────────────────────────────────────
    private static void MapMonitorEndpoints(IEndpointRouteBuilder app)
    {
        var monitors = app.MapGroup("/api/v1/governance/monitors");

        monitors.MapPost("/", async (
            CreateDashboardMonitorFeature.Command command,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");

        monitors.MapGet("/", async (
            Guid dashboardId, string tenantId,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListDashboardMonitorsFeature.Query(dashboardId, tenantId), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }

    // ── V3.8 — Template Marketplace ────────────────────────────────────────────
    private static void MapTemplatesEndpoints(IEndpointRouteBuilder app)
    {
        var templates = app.MapGroup("/api/v1/governance/dashboard-templates");

        templates.MapGet("/", async (
            string tenantId, string? persona, string? category,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListDashboardTemplatesFeature.Query(tenantId, persona, category), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        templates.MapPost("/{id:guid}/instantiate", async (
            Guid id, InstantiateTemplateFeature.Command command,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var cmd = command with { TemplateId = id };
            var result = await sender.Send(cmd, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:write");
    }

    // ── V3.10 — Persona Home ───────────────────────────────────────────────────
    private static void MapPersonaHomeEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/governance/persona-home", async (
            string tenantId, string userId, string persona,
            ISender sender, IErrorLocalizer localizer, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPersonaHomeFeature.Query(tenantId, userId, persona), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }
}
