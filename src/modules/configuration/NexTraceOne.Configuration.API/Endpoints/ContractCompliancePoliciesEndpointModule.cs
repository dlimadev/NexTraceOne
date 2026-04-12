using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreatePolicyFeature = NexTraceOne.Configuration.Application.Features.CreateContractCompliancePolicy.CreateContractCompliancePolicy;
using ListPoliciesFeature = NexTraceOne.Configuration.Application.Features.ListContractCompliancePolicies.ListContractCompliancePolicies;
using GetEffectivePolicyFeature = NexTraceOne.Configuration.Application.Features.GetEffectiveCompliancePolicy.GetEffectiveCompliancePolicy;
using DeletePolicyFeature = NexTraceOne.Configuration.Application.Features.DeleteContractCompliancePolicy.DeleteContractCompliancePolicy;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API de políticas de compliance contratual.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST   /api/v1/contract-compliance-policies              — cria política de compliance
/// - GET    /api/v1/contract-compliance-policies               — lista políticas com filtro por âmbito
/// - GET    /api/v1/contract-compliance-policies/effective     — resolve política efetiva por cascata
/// - DELETE /api/v1/contract-compliance-policies/{id}          — remove política de compliance
///
/// Política de autorização:
/// - Criação e remoção exigem "config:write".
/// - Consultas exigem "config:read".
/// </summary>
public sealed class ContractCompliancePoliciesEndpointModule
{
    /// <summary>Registra os endpoints de políticas de compliance no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contract-compliance-policies");

        // POST /api/v1/contract-compliance-policies
        group.MapPost("/", async (
            CreatePolicyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("config:write");

        // GET /api/v1/contract-compliance-policies?scope=
        group.MapGet("/", async (
            int? scope,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListPoliciesFeature.Query(scope),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("config:read");

        // GET /api/v1/contract-compliance-policies/effective?serviceId=&teamId=&environmentName=
        group.MapGet("/effective", async (
            string? serviceId,
            string? teamId,
            string? environmentName,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetEffectivePolicyFeature.Query(serviceId, teamId, environmentName),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("config:read");

        // DELETE /api/v1/contract-compliance-policies/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DeletePolicyFeature.Command(id),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("config:write");
    }
}
