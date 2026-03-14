using Microsoft.AspNetCore.Builder;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ActivateLicenseFeature = NexTraceOne.Licensing.Application.Features.ActivateLicense.ActivateLicense;
using AlertLicenseThresholdFeature = NexTraceOne.Licensing.Application.Features.AlertLicenseThreshold.AlertLicenseThreshold;
using CheckCapabilityFeature = NexTraceOne.Licensing.Application.Features.CheckCapability.CheckCapability;
using ConvertTrialFeature = NexTraceOne.Licensing.Application.Features.ConvertTrial.ConvertTrial;
using ExtendTrialFeature = NexTraceOne.Licensing.Application.Features.ExtendTrial.ExtendTrial;
using GetLicenseHealthFeature = NexTraceOne.Licensing.Application.Features.GetLicenseHealth.GetLicenseHealth;
using GetLicenseStatusFeature = NexTraceOne.Licensing.Application.Features.GetLicenseStatus.GetLicenseStatus;
using GetTelemetryConsentFeature = NexTraceOne.Licensing.Application.Features.GetTelemetryConsent.GetTelemetryConsent;
using IssueLicenseFeature = NexTraceOne.Licensing.Application.Features.IssueLicense.IssueLicense;
using ListLicensesFeature = NexTraceOne.Licensing.Application.Features.ListLicenses.ListLicenses;
using RehostLicenseFeature = NexTraceOne.Licensing.Application.Features.RehostLicense.RehostLicense;
using RevokeLicenseFeature = NexTraceOne.Licensing.Application.Features.RevokeLicense.RevokeLicense;
using StartTrialFeature = NexTraceOne.Licensing.Application.Features.StartTrial.StartTrial;
using TrackUsageMetricFeature = NexTraceOne.Licensing.Application.Features.TrackUsageMetric.TrackUsageMetric;
using UpdateTelemetryConsentFeature = NexTraceOne.Licensing.Application.Features.UpdateTelemetryConsent.UpdateTelemetryConsent;
using VerifyLicenseOnStartupFeature = NexTraceOne.Licensing.Application.Features.VerifyLicenseOnStartup.VerifyLicenseOnStartup;

namespace NexTraceOne.Licensing.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Licensing.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Grupos de endpoints:
/// - Tenant licensing: activate, verify, status, capabilities, usage, thresholds, health
/// - Trial: start, extend, convert
/// - Telemetry consent: get, update (gestão de consentimento LGPD/GDPR)
/// - Vendor operations: issue, revoke, rehost, list (backoffice interno NexTraceOne)
/// </summary>
public sealed class LicensingEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/licensing");

        // ─── Tenant licensing core ──────────────────────────────────

        group.MapPost("/activate", async (
            ActivateLicenseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/verify", async (
            string licenseKey,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifyLicenseOnStartupFeature.Query(licenseKey), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/status", async (
            string licenseKey,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetLicenseStatusFeature.Query(licenseKey), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/capabilities/{capabilityCode}", async (
            string licenseKey,
            string capabilityCode,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CheckCapabilityFeature.Query(licenseKey, capabilityCode), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/usage", async (
            TrackUsageMetricFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/thresholds", async (
            string licenseKey,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new AlertLicenseThresholdFeature.Query(licenseKey), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ─── Trial ──────────────────────────────────────────────────

        group.MapPost("/trial/start", async (
            StartTrialFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/trial/extend", async (
            ExtendTrialFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/trial/convert", async (
            ConvertTrialFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ─── Observabilidade ────────────────────────────────────────

        group.MapGet("/health", async (
            string licenseKey,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetLicenseHealthFeature.Query(licenseKey), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ─── Telemetry consent (LGPD/GDPR) ─────────────────────────

        group.MapGet("/telemetry-consent", async (
            string licenseKey,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetTelemetryConsentFeature.Query(licenseKey), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/telemetry-consent", async (
            UpdateTelemetryConsentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // ─── Vendor operations (backoffice interno) ─────────────────

        group.MapPost("/vendor/issue", async (
            IssueLicenseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("licensing:vendor:license:create");

        group.MapPost("/vendor/revoke", async (
            RevokeLicenseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("licensing:vendor:license:revoke");

        group.MapPost("/vendor/rehost", async (
            RehostLicenseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("licensing:vendor:license:rehost");

        group.MapGet("/vendor/licenses", async (
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListLicensesFeature.Query(page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("licensing:vendor:license:read");
    }
}
