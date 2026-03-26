using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateSoapDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateSoapDraft.CreateSoapDraft;
using GetSoapContractDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetSoapContractDetail.GetSoapContractDetail;
using ImportWsdlContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportWsdlContract.ImportWsdlContract;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API específicos do workflow SOAP/WSDL no módulo Contracts.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST /api/v1/contracts/wsdl/import      — importa WSDL e extrai metadados SOAP
/// - POST /api/v1/contracts/drafts/soap       — cria draft SOAP com metadados específicos
/// - GET  /api/v1/contracts/{id}/soap-detail  — consulta detalhe SOAP de versão de contrato
///
/// Política de autorização:
/// - Importação e criação exigem "contracts:write".
/// - Consulta exige "contracts:read".
/// </summary>
public sealed class SoapContractEndpointModule
{
    /// <summary>Registra os endpoints SOAP/WSDL no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        // ── Importação de WSDL ──────────────────────────────────────

        app.MapPost("/api/v1/contracts/wsdl/import", async (
            ImportWsdlContractFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/contracts/{response.ContractVersionId}/soap-detail",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Criação de Draft SOAP ───────────────────────────────────

        app.MapPost("/api/v1/contracts/drafts/soap", async (
            CreateSoapDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                response => $"/api/v1/contracts/drafts/{response.DraftId}",
                localizer);
        }).RequirePermission("contracts:write");

        // ── Detalhe SOAP de Versão de Contrato ──────────────────────

        app.MapGet("/api/v1/contracts/{contractVersionId:guid}/soap-detail", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetSoapContractDetailFeature.Query(contractVersionId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
