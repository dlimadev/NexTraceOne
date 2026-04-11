using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateIncidentWithChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetCorrelatedChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentEvidence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentMitigation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentSummary;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.FindSimilarIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentImpactAssessment;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRootCauseSuggestion;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetUnifiedTimeline;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByService;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidentsByTeam;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RefreshIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.SelectMitigationPlaybook;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.TriageIncident;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetOnCallIntelligence;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints.Endpoints;

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
        var group = app.MapGroup("/api/v1/incidents").RequireRateLimiting("operations");

        // ── POST /api/v1/incidents — Criação real de incidente ──
        group.MapPost("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CreateIncident.Command request,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(request, cancellationToken);
            return result.ToCreatedResult(response => $"/api/v1/incidents/{response.IncidentId}", localizer);
        })
        .RequirePermission("operations:incidents:write")
        .WithName("CreateIncident")
        .WithSummary("Create operational incident and compute initial correlation");

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

        // ── GET /api/v1/incidents/timeline — Timeline unificada ──
        group.MapGet("/timeline", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? serviceName,
            string? systemName,
            string? environment,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetUnifiedTimeline.Query(
                serviceName, systemName, environment, from, to, pageSize, page);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetUnifiedTimeline")
        .WithSummary("Get unified timeline of incidents and legacy events");

        // ── GET /api/v1/incidents/{incidentId} — Detalhe do incidente ──
        group.MapGet("/{incidentId:guid}", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentDetail.Query(incidentId.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentDetail")
        .WithSummary("Get consolidated incident detail");

        // ── GET /api/v1/incidents/{incidentId}/correlation — Correlação ──
        group.MapGet("/{incidentId:guid}/correlation", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentCorrelation.Query(incidentId.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentCorrelation")
        .WithSummary("Get incident correlation with changes and services");

        // ── POST /api/v1/incidents/{incidentId}/correlation/refresh — Refresh manual ──
        group.MapPost("/{incidentId:guid}/correlation/refresh", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var command = new RefreshIncidentCorrelation.Command(incidentId.ToString());
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:write")
        .WithName("RefreshIncidentCorrelation")
        .WithSummary("Recompute incident correlation on demand");

        // ── GET /api/v1/incidents/{incidentId}/evidence — Evidências ──
        group.MapGet("/{incidentId:guid}/evidence", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentEvidence.Query(incidentId.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentEvidence")
        .WithSummary("Get incident evidence and operational signals");

        // ── GET /api/v1/incidents/{incidentId}/mitigation — Mitigação e runbooks ──
        group.MapGet("/{incidentId:guid}/mitigation", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentMitigation.Query(incidentId.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentMitigation")
        .WithSummary("Get incident mitigation actions and runbooks");

        // ── Scoped views: by service and by team ──

        // ── POST /api/v1/incidents/{id}/correlate — Motor de correlação dinâmica ──
        group.MapPost("/{incidentId:guid}/correlate", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CorrelateRequest? request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new CorrelateIncidentWithChanges.Command(incidentId, request?.TimeWindowHours);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:write")
        .WithName("CorrelateIncidentWithChanges")
        .WithSummary("Trigger dynamic correlation engine to link incident with changes");

        // ── GET /api/v1/incidents/{id}/correlated-changes — Leitura de correlações persistidas ──
        group.MapGet("/{incidentId:guid}/correlated-changes", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetCorrelatedChanges.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetCorrelatedChanges")
        .WithSummary("Retrieve persisted dynamic correlations for an incident");

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

        // ── GET /api/v1/incidents/{id}/triage — Auto-triage de incidente ──
        group.MapGet("/{id:guid}/triage", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid id,
            CancellationToken cancellationToken = default) =>
        {
            var query = new TriageIncident.Query(id.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("TriageIncident")
        .WithSummary("Get AI-powered auto-triage suggestion for an incident");

        // ── GET /api/v1/incidents/{id}/root-cause — Sugestão de causa raiz ──
        group.MapGet("/{id:guid}/root-cause", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid id,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetRootCauseSuggestion.Query(id.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetRootCauseSuggestion")
        .WithSummary("Get AI-assisted root cause suggestion based on change correlation");

        // ── GET /api/v1/incidents/{id}/impact — Avaliação de impacto ──
        group.MapGet("/{id:guid}/impact", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid id,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetIncidentImpactAssessment.Query(id.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetIncidentImpactAssessment")
        .WithSummary("Get impact assessment: affected services, contracts and blast radius");

        // ── GET /api/v1/incidents/{id}/similar — Incidentes semelhantes ──
        group.MapGet("/{id:guid}/similar", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid id,
            int lookbackDays = 90,
            int maxResults = 10,
            CancellationToken cancellationToken = default) =>
        {
            var query = new FindSimilarIncidents.Query(id.ToString(), lookbackDays, maxResults);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("FindSimilarIncidents")
        .WithSummary("Find similar incidents in the last N days based on service, type and correlation patterns");

        // ── GET /api/v1/incidents/{id}/mitigation-playbook — Playbook auto-selecionado ──
        group.MapGet("/{id:guid}/mitigation-playbook", async (
            ISender sender,
            IErrorLocalizer localizer,
            Guid id,
            CancellationToken cancellationToken = default) =>
        {
            var query = new SelectMitigationPlaybook.Query(id.ToString());
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("SelectMitigationPlaybook")
        .WithSummary("Auto-select the best mitigation playbook (runbook) for an incident based on triage context");

        // ── GET /api/v1/incidents/on-call-intelligence — On-Call Intelligence ──
        group.MapGet("/on-call-intelligence", async (
            ISender sender,
            IErrorLocalizer localizer,
            string teamId,
            int periodDays = 30,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(new GetOnCallIntelligence.Query(teamId, periodDays), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetOnCallIntelligence")
        .WithSummary("On-call intelligence: incident distribution, fatigue indicators and recommendations for a team");
    }
}

/// <summary>Corpo opcional do pedido de correlação dinâmica.</summary>
internal sealed record CorrelateRequest(int? TimeWindowHours);
