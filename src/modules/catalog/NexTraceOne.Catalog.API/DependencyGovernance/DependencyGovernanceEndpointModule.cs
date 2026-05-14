using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.CheckDependencyPolicies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.CompareDependencyVersions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.DetectLicenseConflicts;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GenerateSbom;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetDependencyHealthDashboard;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetServiceDependencyProfile;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetTemplateDependencyHealth;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.ListVulnerableDependencies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.ScanServiceDependencies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.SuggestDependencyUpgrades;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.API.DependencyGovernance;

/// <summary>
/// Endpoints de Dependency Governance — saúde de dependências, SBOM e políticas.
/// Persona primária: Engineer, Platform Admin, Architect, Auditor.
/// </summary>
public sealed class DependencyGovernanceEndpointModule
{
    /// <summary>Regista os endpoints de dependency governance no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/dependencies")
            .WithTags("Dependency Governance")
            .RequireRateLimiting("standard");

        group.MapPost("/scan", async (
            ScanServiceDependencies.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:write")
        .WithName("ScanServiceDependencies")
        .WithSummary("Scan a service's project file and update its dependency profile");

        group.MapGet("/{serviceId:guid}", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetServiceDependencyProfile.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("GetServiceDependencyProfile")
        .WithSummary("Get the full dependency profile for a service");

        group.MapGet("/{serviceId:guid}/health", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDependencyHealthDashboard.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("GetDependencyHealthDashboard")
        .WithSummary("Get dependency health dashboard metrics for a service");

        group.MapPost("/{serviceId:guid}/sbom", async (
            Guid serviceId,
            GenerateSbom.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { ServiceId = serviceId }, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:write")
        .WithName("GenerateServiceSbom")
        .WithSummary("Generate SBOM for a service in the requested format");

        group.MapGet("/{serviceId:guid}/policies", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CheckDependencyPolicies.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("CheckDependencyPolicies")
        .WithSummary("Check dependency policy violations for a service");

        group.MapGet("/{serviceId:guid}/upgrades", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SuggestDependencyUpgrades.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("SuggestDependencyUpgrades")
        .WithSummary("Get upgrade suggestions for outdated or vulnerable dependencies");

        group.MapGet("/{serviceId:guid}/licenses", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DetectLicenseConflicts.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("DetectLicenseConflicts")
        .WithSummary("Detect license conflicts in service dependencies");

        group.MapGet("/vulnerable", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            VulnerabilitySeverity minSeverity = VulnerabilitySeverity.High) =>
        {
            var result = await sender.Send(new ListVulnerableDependencies.Query(minSeverity), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("ListVulnerableDependencies")
        .WithSummary("List services with vulnerabilities above the minimum severity threshold");

        group.MapGet("/compare", async (
            Guid serviceIdA,
            Guid serviceIdB,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CompareDependencyVersions.Query(serviceIdA, serviceIdB), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("CompareDependencyVersions")
        .WithSummary("Compare dependencies between two services");

        group.MapGet("/template/{templateId:guid}/health", async (
            Guid templateId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetTemplateDependencyHealth.Query(templateId), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:dependencies:read")
        .WithName("GetTemplateDependencyHealth")
        .WithSummary("Get dependency health summary for all services using a template");
    }
}
