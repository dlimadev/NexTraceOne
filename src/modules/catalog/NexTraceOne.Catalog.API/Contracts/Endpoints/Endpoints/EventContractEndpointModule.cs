using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateEventDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateEventDraft.CreateEventDraft;
using GetEventContractDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetEventContractDetail.GetEventContractDetail;
using ImportAsyncApiContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportAsyncApiContract.ImportAsyncApiContract;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API específicos do workflow Event Contracts / AsyncAPI no módulo Contracts.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST /api/v1/contracts/asyncapi/import        — importa spec AsyncAPI e extrai metadados de evento
/// - POST /api/v1/contracts/drafts/event           — cria draft de evento com metadados AsyncAPI específicos
/// - GET  /api/v1/contracts/{id}/event-detail      — consulta detalhe AsyncAPI de versão de contrato
///
/// Política de autorização:
/// - Importação e criação exigem "contracts:write".
/// - Consulta exige "contracts:read".
/// </summary>
public sealed class EventContractEndpointModule
{
    /// <summary>Registra os endpoints de Event Contracts no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        // ── Importação de spec AsyncAPI ─────────────────────────────

        app.MapPost("/api/v1/contracts/asyncapi/import", async (
            ImportAsyncApiContractFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/contracts/{response.ContractVersionId}/event-detail",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Criação de Draft de Evento ──────────────────────────────

        app.MapPost("/api/v1/contracts/drafts/event", async (
            CreateEventDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/contracts/drafts/{response.DraftId}",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Detalhe AsyncAPI de Versão de Contrato ──────────────────

        app.MapGet("/api/v1/contracts/{contractVersionId:guid}/event-detail", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetEventContractDetailFeature.Query(contractVersionId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
