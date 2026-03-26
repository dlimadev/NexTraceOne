using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateBackgroundServiceDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateBackgroundServiceDraft.CreateBackgroundServiceDraft;
using GetBackgroundServiceContractDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetBackgroundServiceContractDetail.GetBackgroundServiceContractDetail;
using RegisterBackgroundServiceContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.RegisterBackgroundServiceContract.RegisterBackgroundServiceContract;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API específicos do workflow Background Service Contracts no módulo Contracts.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST /api/v1/contracts/background-services/register    — regista um Background Service Contract
/// - POST /api/v1/contracts/drafts/background-service       — cria draft de background service
/// - GET  /api/v1/contracts/{id}/background-service-detail  — consulta detalhe de background service
///
/// Política de autorização:
/// - Registo e criação exigem "contracts:write".
/// - Consulta exige "contracts:read".
/// </summary>
public sealed class BackgroundServiceContractEndpointModule
{
    /// <summary>Registra os endpoints de Background Service Contracts no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        // ── Registo de Background Service Contract ──────────────────

        app.MapPost("/api/v1/contracts/background-services/register", async (
            RegisterBackgroundServiceContractFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/contracts/{response.ContractVersionId}/background-service-detail",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Criação de Draft de Background Service ──────────────────

        app.MapPost("/api/v1/contracts/drafts/background-service", async (
            CreateBackgroundServiceDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/contracts/drafts/{response.DraftId}",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Detalhe de Background Service de Versão de Contrato ─────

        app.MapGet("/api/v1/contracts/{contractVersionId:guid}/background-service-detail", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetBackgroundServiceContractDetailFeature.Query(contractVersionId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
