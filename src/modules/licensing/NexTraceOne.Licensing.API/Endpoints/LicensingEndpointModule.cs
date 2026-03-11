using Microsoft.AspNetCore.Builder;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using ActivateLicenseFeature = NexTraceOne.Licensing.Application.Features.ActivateLicense.ActivateLicense;
using AlertLicenseThresholdFeature = NexTraceOne.Licensing.Application.Features.AlertLicenseThreshold.AlertLicenseThreshold;
using CheckCapabilityFeature = NexTraceOne.Licensing.Application.Features.CheckCapability.CheckCapability;
using GetLicenseStatusFeature = NexTraceOne.Licensing.Application.Features.GetLicenseStatus.GetLicenseStatus;
using TrackUsageMetricFeature = NexTraceOne.Licensing.Application.Features.TrackUsageMetric.TrackUsageMetric;
using VerifyLicenseOnStartupFeature = NexTraceOne.Licensing.Application.Features.VerifyLicenseOnStartup.VerifyLicenseOnStartup;

namespace NexTraceOne.Licensing.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Licensing.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class LicensingEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/licensing");

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
    }
}
