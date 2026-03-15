using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using RunComplianceChecksFeature = NexTraceOne.Governance.Application.Features.RunComplianceChecks.RunComplianceChecks;
using GetComplianceGapsFeature = NexTraceOne.Governance.Application.Features.GetComplianceGaps.GetComplianceGaps;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Compliance Checks — verificações de conformidade e gaps.
/// Permite execução de checks e consulta de gaps por serviço, equipa ou domínio.
/// </summary>
public sealed class ComplianceChecksEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/compliance");

        group.MapGet("/checks", async (
            string? serviceId,
            string? teamId,
            string? domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new RunComplianceChecksFeature.Query(serviceId, teamId, domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:compliance:read");

        group.MapGet("/gaps", async (
            string? teamId,
            string? domainId,
            string? serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetComplianceGapsFeature.Query(teamId, domainId, serviceId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:compliance:read");
    }
}
