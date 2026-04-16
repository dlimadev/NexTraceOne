using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreatePolicyFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CreateApprovalPolicy.CreateApprovalPolicy;
using ListPoliciesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListApprovalPolicies.ListApprovalPolicies;
using DeletePolicyFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.DeleteApprovalPolicy.DeleteApprovalPolicy;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de políticas de aprovação de releases.
/// Expostos em:
///   GET    /api/v1/releases/approval-policies
///   POST   /api/v1/releases/approval-policies
///   DELETE /api/v1/releases/approval-policies/{id}
/// </summary>
internal static class ApprovalPolicyEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        // GET /api/v1/releases/approval-policies
        group.MapGet("/approval-policies", async (
            string? environmentId,
            Guid? serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListPoliciesFeature.Query(environmentId, serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // POST /api/v1/releases/approval-policies
        group.MapPost("/approval-policies", async (
            CreatePolicyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/releases/approval-policies/{r.PolicyId}",
                localizer);
        }).RequirePermission("change-intelligence:write");

        // DELETE /api/v1/releases/approval-policies/{id}
        group.MapDelete("/approval-policies/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeletePolicyFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");
    }
}
