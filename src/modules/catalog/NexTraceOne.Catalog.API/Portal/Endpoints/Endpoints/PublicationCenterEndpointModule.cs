using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetContractPublicationStatusFeature = NexTraceOne.Catalog.Application.Portal.Features.GetContractPublicationStatus.GetContractPublicationStatus;
using GetPublicationCenterEntriesFeature = NexTraceOne.Catalog.Application.Portal.Features.GetPublicationCenterEntries.GetPublicationCenterEntries;
using PublishContractToPortalFeature = NexTraceOne.Catalog.Application.Portal.Features.PublishContractToPortal.PublishContractToPortal;
using WithdrawContractFromPortalFeature = NexTraceOne.Catalog.Application.Portal.Features.WithdrawContractFromPortal.WithdrawContractFromPortal;

namespace NexTraceOne.Catalog.API.Portal.Endpoints.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API do Publication Center no módulo DeveloperPortal.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST /api/v1/publication-center/publish                         — publica um contrato no portal
/// - POST /api/v1/publication-center/{entryId}/withdraw             — retira publicação do portal
/// - GET  /api/v1/publication-center                                — lista entradas do Publication Center
/// - GET  /api/v1/publication-center/contracts/{contractVersionId}/status — estado de publicação de uma versão
///
/// Política de autorização:
/// - Publicação e retirada exigem "contracts:write".
/// - Consulta e listagem exigem "developer-portal:read".
/// </summary>
public sealed class PublicationCenterEndpointModule
{
    /// <summary>Registra os endpoints do Publication Center no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/publication-center");

        // ── Publicar contrato no Developer Portal ───────────────────

        group.MapPost("/publish", async (
            PublishContractToPortalFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/publication-center/contracts/{response.ContractVersionId}/status",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Retirar publicação do portal ───────────────────────────

        group.MapPost("/{entryId:guid}/withdraw", async (
            Guid entryId,
            ICurrentUser currentUser,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new WithdrawContractFromPortalFeature.Command(
                    entryId,
                    currentUser.Id),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // ── Listar entradas do Publication Center ────────────────────

        group.MapGet("/", async (
            string? status,
            Guid? apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new GetPublicationCenterEntriesFeature.Query(status, apiAssetId, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");

        // ── Estado de publicação de uma versão de contrato ──────────

        group.MapGet("/contracts/{contractVersionId:guid}/status", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetContractPublicationStatusFeature.Query(contractVersionId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("developer-portal:read");
    }
}
