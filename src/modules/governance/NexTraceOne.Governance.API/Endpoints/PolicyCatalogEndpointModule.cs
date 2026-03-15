using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListPoliciesFeature = NexTraceOne.Governance.Application.Features.ListPolicies.ListPolicies;
using GetPolicyFeature = NexTraceOne.Governance.Application.Features.GetPolicy.GetPolicy;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints do Policy Catalog — catálogo de políticas de governança enterprise.
/// Disponibiliza CRUD e consulta de políticas.
/// </summary>
public sealed class PolicyCatalogEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/policies");

        group.MapGet("/", async (
            string? category,
            string? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListPoliciesFeature.Query(category, status);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:policies:read");

        group.MapGet("/{policyId}", async (
            string policyId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPolicyFeature.Query(policyId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:policies:read");
    }
}
