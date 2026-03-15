using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListEvidencePackagesFeature = NexTraceOne.Governance.Application.Features.ListEvidencePackages.ListEvidencePackages;
using GetEvidencePackageFeature = NexTraceOne.Governance.Application.Features.GetEvidencePackage.GetEvidencePackage;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Evidence Packages — pacotes de evidência para auditoria e compliance.
/// Permite consulta, detalhe e exportação de evidências.
/// </summary>
public sealed class EvidencePackagesEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/evidence");

        group.MapGet("/packages", async (
            string? scope,
            string? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListEvidencePackagesFeature.Query(scope, status);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:evidence:read");

        group.MapGet("/packages/{packageId}", async (
            string packageId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEvidencePackageFeature.Query(packageId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:evidence:read");
    }
}
