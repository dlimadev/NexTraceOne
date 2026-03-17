using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints.Endpoints;

/// <summary>
/// Endpoints de Operational Runbooks.
/// Fornece acesso a runbooks operacionais e procedimentos de mitigação.
/// Integra-se com Service Catalog, Incident Correlation e Source of Truth.
/// </summary>
public sealed class RunbookEndpointModule
{
    /// <summary>Mapeia os endpoints de runbooks no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/runbooks")
            .WithTags("Runbooks")
            .WithDescription("Operational runbooks and mitigation procedures");

        // ── GET /api/v1/runbooks — Listagem filtrada de runbooks ──
        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? serviceId,
            string? incidentType,
            string? search,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ListRunbooks.Query(serviceId, incidentType, search);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runbooks:read")
        .WithName("ListRunbooks")
        .WithSummary("List runbooks with optional filters");

        // ── GET /api/v1/runbooks/{runbookId} — Detalhe do runbook ──
        group.MapGet("/{runbookId}", async (
            ISender sender,
            IErrorLocalizer localizer,
            string runbookId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetRunbookDetail.Query(runbookId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runbooks:read")
        .WithName("GetRunbookDetail")
        .WithSummary("Get runbook detail and mitigation procedures");
    }
}
