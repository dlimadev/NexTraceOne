using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using ImportExternalChangeRequestFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ImportExternalChangeRequest.ImportExternalChangeRequest;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints da bridge de pedidos de mudança externos (ServiceNow, Jira, AzureDevOps, Generic).
/// Expostos em:
///   POST /api/v1/changes/external-change-requests          — importação de CR externo
///   GET  /api/v1/changes/external-change-requests?status=&amp;serviceId= — listagem por estado/serviço
/// </summary>
internal static class ExternalChangeBridgeEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/changes/external-change-requests");

        // POST /api/v1/changes/external-change-requests
        // Importa um CR externo de ServiceNow, Jira, AzureDevOps ou Generic
        group.MapPost("/", async (
            ImportExternalChangeRequestFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/changes/external-change-requests/{r.Id}",
                localizer);
        }).RequirePermission("change-intelligence:write");

        // GET /api/v1/changes/external-change-requests?status=&serviceId=
        // Placeholder — retorna 200 com lista vazia; pesquisa real via feature dedicada na próxima wave
        group.MapGet("/", async (
            string? status,
            Guid? serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            // Retorno directo de lista vazia enquanto não existe query dedicada.
            // A feature ImportExternalChangeRequest garante a persistência e o repositório suporta as queries.
            await Task.CompletedTask;
            return Microsoft.AspNetCore.Http.Results.Ok(Array.Empty<object>());
        }).RequirePermission("change-intelligence:read");
    }
}
