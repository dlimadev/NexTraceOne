using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using CreateCustomDashboardFeature = NexTraceOne.Governance.Application.Features.CreateCustomDashboard.CreateCustomDashboard;
using GetCustomDashboardFeature = NexTraceOne.Governance.Application.Features.GetCustomDashboard.GetCustomDashboard;
using ListCustomDashboardsFeature = NexTraceOne.Governance.Application.Features.ListCustomDashboards.ListCustomDashboards;
using RecordTechnicalDebtFeature = NexTraceOne.Governance.Application.Features.RecordTechnicalDebt.RecordTechnicalDebt;
using GetTechnicalDebtSummaryFeature = NexTraceOne.Governance.Application.Features.GetTechnicalDebtSummary.GetTechnicalDebtSummary;

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
