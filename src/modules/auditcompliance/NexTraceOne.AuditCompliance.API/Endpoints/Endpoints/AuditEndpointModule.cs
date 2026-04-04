using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateAuditCampaignFeature = NexTraceOne.AuditCompliance.Application.Features.CreateAuditCampaign.CreateAuditCampaign;
using CreateCompliancePolicyFeature = NexTraceOne.AuditCompliance.Application.Features.CreateCompliancePolicy.CreateCompliancePolicy;
using ExportAuditReportFeature = NexTraceOne.AuditCompliance.Application.Features.ExportAuditReport.ExportAuditReport;
using GetAuditCampaignFeature = NexTraceOne.AuditCompliance.Application.Features.GetAuditCampaign.GetAuditCampaign;
using GetAuditTrailFeature = NexTraceOne.AuditCompliance.Application.Features.GetAuditTrail.GetAuditTrail;
using GetCompliancePolicyFeature = NexTraceOne.AuditCompliance.Application.Features.GetCompliancePolicy.GetCompliancePolicy;
using GetComplianceReportFeature = NexTraceOne.AuditCompliance.Application.Features.GetComplianceReport.GetComplianceReport;
using GetRetentionPoliciesFeature = NexTraceOne.AuditCompliance.Application.Features.GetRetentionPolicies.GetRetentionPolicies;
using ListAuditCampaignsFeature = NexTraceOne.AuditCompliance.Application.Features.ListAuditCampaigns.ListAuditCampaigns;
using ListCompliancePoliciesFeature = NexTraceOne.AuditCompliance.Application.Features.ListCompliancePolicies.ListCompliancePolicies;
using ListComplianceResultsFeature = NexTraceOne.AuditCompliance.Application.Features.ListComplianceResults.ListComplianceResults;
using ApplyRetentionFeature = NexTraceOne.AuditCompliance.Application.Features.ApplyRetention.ApplyRetention;
using ConfigureRetentionFeature = NexTraceOne.AuditCompliance.Application.Features.ConfigureRetention.ConfigureRetention;
using RecordAuditEventFeature = NexTraceOne.AuditCompliance.Application.Features.RecordAuditEvent.RecordAuditEvent;
using RecordComplianceResultFeature = NexTraceOne.AuditCompliance.Application.Features.RecordComplianceResult.RecordComplianceResult;
using SearchAuditLogFeature = NexTraceOne.AuditCompliance.Application.Features.SearchAuditLog.SearchAuditLog;
using VerifyChainIntegrityFeature = NexTraceOne.AuditCompliance.Application.Features.VerifyChainIntegrity.VerifyChainIntegrity;
using GetComplianceFrameworkSummaryFeature = NexTraceOne.AuditCompliance.Application.Features.GetComplianceFrameworkSummary.GetComplianceFrameworkSummary;
using EvaluateContinuousComplianceFeature = NexTraceOne.AuditCompliance.Application.Features.EvaluateContinuousCompliance.EvaluateContinuousCompliance;
using GetComplianceDashboardFeature = NexTraceOne.AuditCompliance.Application.Features.GetComplianceDashboard.GetComplianceDashboard;
using ExportComplianceEvidencesFeature = NexTraceOne.AuditCompliance.Application.Features.ExportComplianceEvidences.ExportComplianceEvidences;

namespace NexTraceOne.AuditCompliance.API.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Audit.
/// </summary>
public sealed class AuditEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit");

        group.MapPost("/events", async (
            RecordAuditEventFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:events:write");

        group.MapGet("/trail", async (
            string resourceType,
            string resourceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAuditTrailFeature.Query(resourceType, resourceId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:trail:read");

        group.MapGet("/search", async (
            string? sourceModule,
            string? actionType,
            string? correlationId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string? resourceType,
            string? resourceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new SearchAuditLogFeature.Query(
                    sourceModule,
                    actionType,
                    correlationId,
                    from,
                    to,
                    page,
                    pageSize,
                    resourceType,
                    resourceId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:trail:read");

        group.MapGet("/verify-chain", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifyChainIntegrityFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:trail:read");

        group.MapGet("/report", async (
            DateTimeOffset from,
            DateTimeOffset to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportAuditReportFeature.Query(from, to), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:reports:read");

        group.MapGet("/compliance", async (
            DateTimeOffset from,
            DateTimeOffset to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetComplianceReportFeature.Query(from, to), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        // --- Retention Policies ---
        var retentionGroup = app.MapGroup("/api/v1/audit/retention");

        retentionGroup.MapPost("/policies", async (
            ConfigureRetentionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:write");

        retentionGroup.MapGet("/policies", async (
            bool? activeOnly,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRetentionPoliciesFeature.Query(activeOnly), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        retentionGroup.MapPost("/apply", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ApplyRetentionFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:write");

        // --- Compliance Policies ---
        var policiesGroup = app.MapGroup("/api/v1/audit/compliance/policies");

        policiesGroup.MapPost("/", async (
            CreateCompliancePolicyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:write");

        policiesGroup.MapGet("/", async (
            bool? isActive,
            string? category,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListCompliancePoliciesFeature.Query(isActive, category), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        policiesGroup.MapGet("/{policyId:guid}", async (
            Guid policyId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetCompliancePolicyFeature.Query(policyId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        // --- Audit Campaigns ---
        var campaignsGroup = app.MapGroup("/api/v1/audit/campaigns");

        campaignsGroup.MapPost("/", async (
            CreateAuditCampaignFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:write");

        campaignsGroup.MapGet("/", async (
            CampaignStatus? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListAuditCampaignsFeature.Query(status), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        campaignsGroup.MapGet("/{campaignId:guid}", async (
            Guid campaignId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAuditCampaignFeature.Query(campaignId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        // --- Compliance Results ---
        var resultsGroup = app.MapGroup("/api/v1/audit/compliance/results");

        resultsGroup.MapPost("/", async (
            RecordComplianceResultFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:write");

        resultsGroup.MapGet("/", async (
            Guid? policyId,
            Guid? campaignId,
            ComplianceOutcome? outcome,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListComplianceResultsFeature.Query(policyId, campaignId, outcome), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read");

        // ── Compliance as Code (Phase 3.5) ────────────────────────────────────────────────

        // GET /api/v1/audit/compliance/dashboard — Dashboard de compliance contínuo
        var dashboardGroup = app.MapGroup("/api/v1/audit/compliance");

        dashboardGroup.MapGet("/dashboard", async (
            Guid tenantId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetComplianceDashboardFeature.Query(tenantId, from, to), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read")
        .WithName("GetComplianceDashboard")
        .WithSummary("Get continuous compliance dashboard with status by category and critical gaps");

        // GET /api/v1/audit/compliance/framework/{framework} — Resumo por framework
        dashboardGroup.MapGet("/framework/{framework}", async (
            string framework,
            Guid tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetComplianceFrameworkSummaryFeature.Query(framework, tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read")
        .WithName("GetComplianceFrameworkSummary")
        .WithSummary("Get compliance status summary for a specific regulatory framework (SOC2, ISO27001, LGPD, GDPR, PCI-DSS)");

        // POST /api/v1/audit/compliance/evaluate — Avaliação contínua de recurso
        dashboardGroup.MapPost("/evaluate", async (
            EvaluateContinuousComplianceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:write")
        .WithName("EvaluateContinuousCompliance")
        .WithSummary("Evaluate a resource against all active compliance policies and persist results");

        // GET /api/v1/audit/compliance/evidences/export — Exportação de evidências para auditores
        dashboardGroup.MapGet("/evidences/export", async (
            Guid tenantId,
            string? framework,
            string? category,
            DateTimeOffset? from,
            DateTimeOffset? to,
            bool includeCompliant,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ExportComplianceEvidencesFeature.Query(tenantId, framework, category, from, to, includeCompliant),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("audit:compliance:read")
        .WithName("ExportComplianceEvidences")
        .WithSummary("Export compliance evidence pack grouped by framework and policy for auditor review");
    }
}
