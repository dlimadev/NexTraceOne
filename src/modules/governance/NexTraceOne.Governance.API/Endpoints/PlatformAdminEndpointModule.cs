using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetSamlSsoConfigFeature = NexTraceOne.Governance.Application.Features.GetSamlSsoConfig.GetSamlSsoConfig;
using GetMtlsManagerFeature = NexTraceOne.Governance.Application.Features.GetMtlsManager.GetMtlsManager;
using GetAdminBackupFeature = NexTraceOne.Governance.Application.Features.GetAdminBackup.GetAdminBackup;
using GetSupportBundlesFeature = NexTraceOne.Governance.Application.Features.GetSupportBundles.GetSupportBundles;
using GetStartupReportFeature = NexTraceOne.Governance.Application.Features.GetStartupReport.GetStartupReport;
using GetResourceBudgetFeature = NexTraceOne.Governance.Application.Features.GetResourceBudget.GetResourceBudget;
using GetElasticsearchManagerFeature = NexTraceOne.Governance.Application.Features.GetElasticsearchManager.GetElasticsearchManager;
using GetPlatformAlertRulesFeature = NexTraceOne.Governance.Application.Features.GetPlatformAlertRules.GetPlatformAlertRules;
using GetRestorePointsFeature = NexTraceOne.Governance.Application.Features.GetRestorePoints.GetRestorePoints;
using GetGreenOpsReportFeature = NexTraceOne.Governance.Application.Features.GetGreenOpsReport.GetGreenOpsReport;
using GetAiGovernorStatusFeature = NexTraceOne.Governance.Application.Features.GetAiGovernorStatus.GetAiGovernorStatus;
using GetAiGovernanceDashboardFeature = NexTraceOne.Governance.Application.Features.GetAiGovernanceDashboard.GetAiGovernanceDashboard;
using GetProxyConfigFeature = NexTraceOne.Governance.Application.Features.GetProxyConfig.GetProxyConfig;
using GetExternalHttpAuditFeature = NexTraceOne.Governance.Application.Features.GetExternalHttpAudit.GetExternalHttpAudit;
using GetEnvironmentPoliciesFeature = NexTraceOne.Governance.Application.Features.GetEnvironmentPolicies.GetEnvironmentPolicies;
using GetNonProdSchedulesFeature = NexTraceOne.Governance.Application.Features.GetNonProdSchedules.GetNonProdSchedules;
using GetCapacityForecastFeature = NexTraceOne.Governance.Application.Features.GetCapacityForecast.GetCapacityForecast;
using GetDemoSeedStatusFeature = NexTraceOne.Governance.Application.Features.GetDemoSeedStatus.GetDemoSeedStatus;
using GetGracefulShutdownConfigFeature = NexTraceOne.Governance.Application.Features.GetGracefulShutdownConfig.GetGracefulShutdownConfig;
using GetSessionSecurityConfigFeature = NexTraceOne.Governance.Application.Features.GetSessionSecurityConfig.GetSessionSecurityConfig;
using GetRightsizingReportFeature = NexTraceOne.Governance.Application.Features.GetRightsizingReport.GetRightsizingReport;
using GetObservabilityModeFeature = NexTraceOne.Governance.Application.Features.GetObservabilityMode.GetObservabilityMode;
using GetCompliancePacksFeature = NexTraceOne.Governance.Application.Features.GetCompliancePacks.GetCompliancePacks;
using GetHardwareAssessmentFeature = NexTraceOne.Governance.Application.Features.GetHardwareAssessment.GetHardwareAssessment;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Platform Admin — gestão de SSO, mTLS, backups, bundles de suporte,
/// recursos, Elasticsearch, alertas, recovery, GreenOps, IA, proxy, auditoria HTTP,
/// políticas de ambiente, agendas não-produtivas, capacidade, demo seed,
/// graceful shutdown, segurança de sessão, rightsizing, modo de observabilidade e conformidade.
/// Destinados exclusivamente a Platform Admins.
/// </summary>
public sealed class PlatformAdminEndpointModule
{
    /// <summary>Registra endpoints de administração da plataforma no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin");

        // ── SAML SSO ──────────────────────────────────────────────────────────────
        admin.MapGet("/saml-sso", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSamlSsoConfigFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/saml-sso", async (
            GetSamlSsoConfigFeature.UpdateSamlSsoConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        admin.MapPost("/saml-sso/test", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSamlSsoConfigFeature.TestSamlConnection(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── mTLS ──────────────────────────────────────────────────────────────────
        admin.MapGet("/mtls", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetMtlsManagerFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPost("/mtls/certificates/{certId}/revoke", async (
            string certId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetMtlsManagerFeature.RevokeMtlsCertificate(certId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        admin.MapPut("/mtls/policy", async (
            GetMtlsManagerFeature.UpdateMtlsPolicy command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Backup ────────────────────────────────────────────────────────────────
        admin.MapGet("/backup", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAdminBackupFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/backup/schedule", async (
            GetAdminBackupFeature.UpdateBackupSchedule command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        admin.MapPost("/backup/run", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAdminBackupFeature.RunBackupNow(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Support Bundles ───────────────────────────────────────────────────────
        admin.MapGet("/support-bundles", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSupportBundlesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPost("/support-bundles", async (
            GetSupportBundlesFeature.GenerateSupportBundle command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Startup Report ────────────────────────────────────────────────────────
        admin.MapGet("/startup-report", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetStartupReportFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        // ── Resource Budget ───────────────────────────────────────────────────────
        admin.MapGet("/resource-budget", async (
            Guid? tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetResourceBudgetFeature.Query(tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/resource-budget", async (
            GetResourceBudgetFeature.UpdateResourceBudget command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Elasticsearch ─────────────────────────────────────────────────────────
        admin.MapGet("/elasticsearch", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetElasticsearchManagerFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/elasticsearch/ilm", async (
            GetElasticsearchManagerFeature.UpdateElasticsearchIlm command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Platform Alerts ───────────────────────────────────────────────────────
        admin.MapGet("/platform-alerts", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPlatformAlertRulesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/platform-alerts/{ruleId}", async (
            string ruleId,
            GetPlatformAlertRulesFeature.UpdatePlatformAlertRule command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { RuleId = ruleId }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Recovery / Restore Points ─────────────────────────────────────────────
        admin.MapGet("/recovery/restore-points", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRestorePointsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPost("/recovery/initiate", async (
            GetRestorePointsFeature.InitiateRecovery command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── GreenOps ──────────────────────────────────────────────────────────────
        admin.MapGet("/greenops", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetGreenOpsReportFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/greenops/config", async (
            GetGreenOpsReportFeature.UpdateGreenOpsConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── AI Governor ───────────────────────────────────────────────────────────
        admin.MapGet("/ai/governor", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAiGovernorStatusFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/ai/governor", async (
            GetAiGovernorStatusFeature.UpdateAiGovernorConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── AI Governance ─────────────────────────────────────────────────────────
        admin.MapGet("/ai/governance", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAiGovernanceDashboardFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/ai/governance/config", async (
            GetAiGovernanceDashboardFeature.UpdateAiGovernanceConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── AI Hardware Assessment ─────────────────────────────────────────────────
        admin.MapGet("/ai/hardware-assessment", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetHardwareAssessmentFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        // ── Proxy Config ──────────────────────────────────────────────────────────
        admin.MapGet("/proxy-config", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetProxyConfigFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/proxy-config", async (
            GetProxyConfigFeature.UpdateProxyConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        admin.MapPost("/proxy-config/test", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetProxyConfigFeature.TestProxyConnectivity(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── External HTTP Audit ───────────────────────────────────────────────────
        admin.MapGet("/external-http-audit", async (
            string? destination,
            string? context,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExternalHttpAuditFeature.Query(
                destination,
                context,
                from,
                to,
                page ?? 1,
                pageSize ?? 25);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        // ── Environment Policies ──────────────────────────────────────────────────
        admin.MapGet("/environment-policies", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetEnvironmentPoliciesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/environment-policies/{policyId}", async (
            string policyId,
            GetEnvironmentPoliciesFeature.UpdateEnvironmentPolicy command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { PolicyId = policyId }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Non-Prod Schedules ────────────────────────────────────────────────────
        admin.MapGet("/nonprod-schedules", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetNonProdSchedulesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/nonprod-schedules/{environmentId}", async (
            string environmentId,
            GetNonProdSchedulesFeature.UpdateNonProdSchedule command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { EnvironmentId = environmentId }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        admin.MapPost("/nonprod-schedules/{environmentId}/override", async (
            string environmentId,
            GetNonProdSchedulesFeature.OverrideNonProdSchedule command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { EnvironmentId = environmentId }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Capacity Forecast ─────────────────────────────────────────────────────
        admin.MapGet("/capacity-forecast", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetCapacityForecastFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        // ── Demo Seed ─────────────────────────────────────────────────────────────
        admin.MapGet("/demo-seed", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDemoSeedStatusFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPost("/demo-seed", async (
            GetDemoSeedStatusFeature.RunDemoSeed command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        admin.MapDelete("/demo-seed", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDemoSeedStatusFeature.ClearDemoSeed(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Graceful Shutdown ─────────────────────────────────────────────────────
        admin.MapGet("/graceful-shutdown", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetGracefulShutdownConfigFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/graceful-shutdown", async (
            GetGracefulShutdownConfigFeature.UpdateGracefulShutdownConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Session Security ──────────────────────────────────────────────────────
        admin.MapGet("/session-security", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSessionSecurityConfigFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/session-security", async (
            GetSessionSecurityConfigFeature.UpdateSessionSecurityConfig command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Rightsizing ───────────────────────────────────────────────────────────
        admin.MapGet("/rightsizing", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetRightsizingReportFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        // ── Observability Mode ────────────────────────────────────────────────────
        admin.MapGet("/observability-mode", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetObservabilityModeFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        admin.MapPut("/observability-mode", async (
            GetObservabilityModeFeature.UpdateObservabilityMode command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:write");

        // ── Compliance Packs ──────────────────────────────────────────────────────
        admin.MapGet("/compliance-packs", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetCompliancePacksFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");
    }
}
