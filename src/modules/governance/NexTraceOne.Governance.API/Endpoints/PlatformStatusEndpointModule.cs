using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetPlatformHealthFeature = NexTraceOne.Governance.Application.Features.GetPlatformHealth.GetPlatformHealth;
using GetPlatformJobsFeature = NexTraceOne.Governance.Application.Features.GetPlatformJobs.GetPlatformJobs;
using GetPlatformQueuesFeature = NexTraceOne.Governance.Application.Features.GetPlatformQueues.GetPlatformQueues;
using GetPlatformConfigFeature = NexTraceOne.Governance.Application.Features.GetPlatformConfig.GetPlatformConfig;
using GetPlatformEventsFeature = NexTraceOne.Governance.Application.Features.GetPlatformEvents.GetPlatformEvents;
using GetPlatformReadinessFeature = NexTraceOne.Governance.Application.Features.GetPlatformReadiness.GetPlatformReadiness;
using GetConfigHealthFeature = NexTraceOne.Governance.Application.Features.GetConfigHealth.GetConfigHealth;
using GetPendingMigrationsFeature = NexTraceOne.Governance.Application.Features.GetPendingMigrations.GetPendingMigrations;
using GetNetworkPolicyFeature = NexTraceOne.Governance.Application.Features.GetNetworkPolicy.GetNetworkPolicy;
using GetTenantSchemasFeature = NexTraceOne.Governance.Application.Features.GetTenantSchemas.GetTenantSchemas;
using ProvisionTenantSchemaFeature = NexTraceOne.Governance.Application.Features.GetTenantSchemas.ProvisionTenantSchema;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Platform Status — saúde, jobs, filas, configuração, readiness e eventos operacionais.
/// Destinados a Platform Admins e operadores para monitorização e diagnóstico da plataforma.
/// </summary>
public sealed class PlatformStatusEndpointModule
{
    /// <summary>Registra endpoints de status da plataforma no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var platform = app.MapGroup("/api/v1/platform");

        platform.MapGet("/health", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPlatformHealthFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/jobs", async (
            string? status,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPlatformJobsFeature.Query(status, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/queues", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPlatformQueuesFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/config", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPlatformConfigFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/events", async (
            string? severity,
            string? subsystem,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPlatformEventsFeature.Query(severity, subsystem, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/readiness", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPlatformReadinessFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/config-health", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetConfigHealthFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/migrations/pending", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPendingMigrationsFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/network-policy", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNetworkPolicyFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapGet("/tenant-schemas", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTenantSchemasFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");

        platform.MapPost("/tenant-schemas/provision", async (
            ProvisionTenantSchemaFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer, statusCode: 201);
        }).RequirePermission("platform:admin:write");
    }
}
