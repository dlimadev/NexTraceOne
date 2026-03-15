using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentSummary;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByTeam;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints;

/// <summary>
/// Endpoints de Incident Correlation &amp; Mitigation.
/// Fornece acesso contextualizado a incidentes com correlação, evidência, mitigação e runbooks.
/// Integra-se com Service Catalog, Change Intelligence, Contract Governance e Source of Truth.
/// </summary>
public sealed class IncidentEndpointModule
{
    /// <summary>Mapeia os endpoints de incidentes no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/incidents");

        // ── GET /api/v1/incidents — Listagem filtrada de incidentes ──
        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? teamId,
            string? serviceId,
            string? environment,
            IncidentSeverity? severity,
            IncidentStatus? status,
            IncidentType? incidentType,
            string? search,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ListIncidents.Query(
                teamId, serviceId, environment, severity, status,
                incidentType, search, from, to, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("ListIncidents")
        .WithSummary("List incidents with contextual filters");

        // ── GET /api/v1/incidents/summary — Resumo agregado ──
        group.MapGet("/summary", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? teamId,
            string? environment,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentSummary.Query(teamId, environment, from, to);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentSummary")
        .WithSummary("Get aggregated incident summary");

        // ── GET /api/v1/incidents/{incidentId} — Detalhe do incidente ──
        group.MapGet("/{incidentId}", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentDetail.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentDetail")
        .WithSummary("Get consolidated incident detail");

        // ── GET /api/v1/incidents/{incidentId}/correlation — Correlação ──
        group.MapGet("/{incidentId}/correlation", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentCorrelation.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentCorrelation")
        .WithSummary("Get incident correlation with changes and services");

        // ── GET /api/v1/incidents/{incidentId}/evidence — Evidências ──
        group.MapGet("/{incidentId}/evidence", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentEvidence.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentEvidence")
        .WithSummary("Get incident evidence and operational signals");

        // ── GET /api/v1/incidents/{incidentId}/mitigation — Mitigação e runbooks ──
        group.MapGet("/{incidentId}/mitigation", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentMitigation.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentMitigation")
        .WithSummary("Get incident mitigation actions and runbooks");

        // ── Scoped views: by service and by team ──

        var servicesGroup = app.MapGroup("/api/v1/services");

        // ── GET /api/v1/services/{serviceId}/incidents — Incidentes por serviço ──
        servicesGroup.MapGet("/{serviceId}/incidents", async (
            ISender sender,
            IErrorLocalizer localizer,
            string serviceId,
            IncidentStatus? status,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ListIncidentsByService.Query(serviceId, status, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("ListIncidentsByService")
        .WithSummary("List incidents by service");

        var teamsGroup = app.MapGroup("/api/v1/teams");

        // ── GET /api/v1/teams/{teamId}/incidents — Incidentes por equipa ──
        teamsGroup.MapGet("/{teamId}/incidents", async (
            ISender sender,
            IErrorLocalizer localizer,
            string teamId,
            IncidentStatus? status,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ListIncidentsByTeam.Query(teamId, status, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("ListIncidentsByTeam")
        .WithSummary("List incidents by team");
    }
}
