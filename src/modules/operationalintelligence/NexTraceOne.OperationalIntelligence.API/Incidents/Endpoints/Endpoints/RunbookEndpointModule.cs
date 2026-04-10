using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateRunbook;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetRunbookDetail;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListRunbooks;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.SuggestRunbooksForIncident;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateRunbook;

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

        // ── GET /api/v1/runbooks/suggest — Sugerir runbooks para um incidente ──
        // NOTA: Mapeado ANTES da rota paramétrica /{runbookId} para evitar que
        // "suggest" seja capturado como runbookId pela rota genérica.
        group.MapGet("/suggest", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? serviceId,
            string? incidentType,
            string? incidentTitle,
            int maxResults = 5,
            CancellationToken cancellationToken = default) =>
        {
            var query = new SuggestRunbooksForIncident.Query(serviceId, incidentType, incidentTitle, maxResults);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runbooks:read")
        .WithName("SuggestRunbooksForIncident")
        .WithSummary("Suggest relevant runbooks for an incident based on service, type and title matching");

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

        // ── POST /api/v1/runbooks — Criar novo runbook ──
        group.MapPost("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CreateRunbook.Command command,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/runbooks/{r.RunbookId}", localizer);
        })
        .RequirePermission("operations:runbooks:write")
        .WithName("CreateRunbook")
        .WithSummary("Create a new operational runbook");

        // ── PUT /api/v1/runbooks/{runbookId} — Atualizar runbook existente ──
        group.MapPut("/{runbookId}", async (
            ISender sender,
            IErrorLocalizer localizer,
            string runbookId,
            UpdateRunbook.Command command,
            CancellationToken cancellationToken = default) =>
        {
            var cmd = command with { RunbookId = Guid.Parse(runbookId) };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:runbooks:write")
        .WithName("UpdateRunbook")
        .WithSummary("Update an existing operational runbook");
    }
}
