using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Ingestion.Api.Security;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using CreateIncidentFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident.CreateIncident;
using GetIncidentDetailFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentDetail.GetIncidentDetail;
using ListIncidentsFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents.ListIncidents;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de incidentes operacionais expostos na Ingestion API.
/// Permite que sistemas externos de alerta (PagerDuty, OpsGenie, Prometheus Alertmanager, APM)
/// criem incidentes no NexTraceOne com correlação automática de mudanças.
/// Também expõe endpoints GET para que sistemas externos consultem o estado de incidentes,
/// integrando o NexTraceOne como source of truth operacional.
/// </summary>
internal static class IncidentEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de incidentes no grupo raiz de incidents.
    /// </summary>
    internal static void Map(RouteGroupBuilder writeGroup, RouteGroupBuilder readGroup)
    {
        MapCreateIncident(writeGroup);
        MapListIncidents(readGroup);
        MapGetIncidentDetail(readGroup);
    }

    private static void MapCreateIncident(RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            HttpContext httpContext,
            CreateIncidentRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(IncidentEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Auditoria de ingestão ─────────────────────────────────────────────
            const string incidentConnectorName = "external-incident-source";
            var connector = await connectorRepo.GetByNameAsync(incidentConnectorName, ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: incidentConnectorName,
                    connectorType: "Alerting",
                    description: "External alerting and incident management systems",
                    provider: "External",
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            var ingestionSource = await sourceRepo.GetByConnectorAndNameAsync(connector.Id, "incidents", ct);
            if (ingestionSource is null)
            {
                ingestionSource = IngestionSource.Create(
                    connectorId: connector.Id,
                    name: "incidents",
                    sourceType: "Webhook",
                    dataDomain: "OperationalIntelligence",
                    description: "Incident events from external alerting systems",
                    endpoint: null,
                    expectedIntervalMinutes: null,
                    utcNow: clock.UtcNow);
                await sourceRepo.AddAsync(ingestionSource, ct);
            }

            var execution = IngestionExecution.Start(connector.Id, ingestionSource.Id, correlationId, clock.UtcNow);

            // ── Dispatch para o domínio ───────────────────────────────────────────
            object? incidentResult = null;
            string processingStatus;

            try
            {
                var command = new CreateIncidentFeature.Command(
                    Title: request.Title,
                    Description: request.Description,
                    IncidentType: request.IncidentType,
                    Severity: request.Severity,
                    ServiceId: request.ServiceId,
                    ServiceDisplayName: request.ServiceDisplayName,
                    OwnerTeam: request.OwnerTeam,
                    ImpactedDomain: request.ImpactedDomain,
                    Environment: request.Environment,
                    DetectedAtUtc: request.DetectedAtUtc);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    incidentResult = new
                    {
                        incidentId = result.Value.IncidentId,
                        reference = result.Value.Reference,
                        status = result.Value.Status.ToString(),
                        severity = result.Value.Severity.ToString(),
                        correlationConfidence = result.Value.CorrelationConfidence.ToString(),
                        hasCorrelatedChanges = result.Value.HasCorrelatedChanges,
                        correlationScore = result.Value.CorrelationScore,
                        correlationReason = result.Value.CorrelationReason
                    };
                    processingStatus = "incident_created";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected incident creation", null, clock.UtcNow);
                    logger.LogWarning(
                        "CreateIncident rejected for service {ServiceId}/{Environment}: {Error}",
                        request.ServiceId, request.Environment, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, null, clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error creating incident for service {ServiceId}", request.ServiceId);
            }

            ingestionSource.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);
            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(ingestionSource, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Incident creation accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                incident = incidentResult
            });
        })
        .WithName("PostCreateIncident")
        .WithSummary("Create an operational incident from an external alerting system")
        .WithDescription(
            "Creates an incident in NexTraceOne from an external alerting system (PagerDuty, OpsGenie, " +
            "Prometheus Alertmanager, APM alert). " +
            "Automatic change correlation is computed on creation to surface related recent changes.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapListIncidents(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            HttpContext httpContext,
            ISender sender,
            ILoggerFactory loggerFactory,
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
            CancellationToken ct = default) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(IncidentEndpoints));

            try
            {
                var query = new ListIncidentsFeature.Query(
                    TeamId: teamId,
                    ServiceId: serviceId,
                    Environment: environment,
                    Severity: severity,
                    Status: status,
                    IncidentType: incidentType,
                    Search: search,
                    From: from,
                    To: to,
                    Page: page,
                    PageSize: pageSize);

                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                logger.LogWarning("ListIncidents query returned failure: {Error}", result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error listing incidents");
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetIncidents")
        .WithSummary("List operational incidents from NexTraceOne")
        .WithDescription(
            "Returns a paginated list of incidents with correlation, severity, status and mitigation context. " +
            "Supports filtering by team, service, environment, severity, status, type and date range.")
        .Produces<ListIncidentsFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGetIncidentDetail(RouteGroupBuilder group)
    {
        group.MapGet("/{incidentId}", async (
            HttpContext httpContext,
            string incidentId,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            IngestionCorrelationHelper.ResolveCorrelationId(httpContext);
            var logger = loggerFactory.CreateLogger(nameof(IncidentEndpoints));

            try
            {
                var query = new GetIncidentDetailFeature.Query(IncidentId: incidentId);
                var result = await sender.Send(query, ct);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                if (result.Error?.Code == "incident.not_found")
                    return Results.NotFound(new { message = result.Error.Message, incidentId });

                logger.LogWarning("GetIncidentDetail failed for {IncidentId}: {Error}", incidentId, result.Error?.Message);
                return Results.Problem(result.Error?.Message ?? "Query failed", statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting incident detail for {IncidentId}", incidentId);
                return Results.Problem("An unexpected error occurred", statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetIncidentDetail")
        .WithSummary("Get detailed information about a specific incident")
        .WithDescription(
            "Returns the full consolidated detail of an incident including timeline, correlated changes, " +
            "evidence, related contracts, runbooks and mitigation. " +
            "Correlation with recent changes is recomputed on each read.")
        .Produces<GetIncidentDetailFeature.Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
