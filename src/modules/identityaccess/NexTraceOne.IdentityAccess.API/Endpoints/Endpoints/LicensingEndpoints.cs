using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetTenantLicenseFeature = NexTraceOne.IdentityAccess.Application.Features.GetTenantLicense.GetTenantLicense;
using ProvisionTenantLicenseFeature = NexTraceOne.IdentityAccess.Application.Features.ProvisionTenantLicense.ProvisionTenantLicense;
using ListAgentRegistrationsFeature = NexTraceOne.IdentityAccess.Application.Features.ListAgentRegistrations.ListAgentRegistrations;
using RecordAgentHeartbeatFeature = NexTraceOne.IdentityAccess.Application.Features.RecordAgentHeartbeat.RecordAgentHeartbeat;

namespace NexTraceOne.IdentityAccess.API.Endpoints.Endpoints;

/// <summary>
/// SaaS-01, SaaS-03, SaaS-04: Endpoints de licensing e agent heartbeat.
/// </summary>
internal static class LicensingEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        // ── Licensing ────────────────────────────────────────────────────

        var licenseGroup = group.MapGroup("/license");

        licenseGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTenantLicenseFeature.Query(), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenant:read");

        licenseGroup.MapPost("/provision", async (
            ProvisionTenantLicenseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(r => $"/api/v1/identity/license/{r.LicenseId}", localizer);
        }).RequirePermission("identity:tenant:write");

        // ── Agent Registrations ──────────────────────────────────────────

        var agentsGroup = group.MapGroup("/agents");

        agentsGroup.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListAgentRegistrationsFeature.Query(), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("identity:tenant:read");

        // Heartbeat endpoint — chamado pelo NexTrace Agent (auth via ApiKey)
        agentsGroup.MapPost("/heartbeat", async (
            RecordAgentHeartbeatFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous(); // Auth via API Key header processada antes deste handler
    }
}
