using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using VerifyComplianceFeature = NexTraceOne.Catalog.Application.Contracts.Features.VerifyContractCompliance.VerifyContractCompliance;
using ListVerificationsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractVerifications.ListContractVerifications;
using GetVerificationDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractVerificationDetail.GetContractVerificationDetail;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API de verificação de compliance contratual.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST /api/v1/contracts/verifications         — executa verificação de compliance contratual
/// - GET  /api/v1/contracts/verifications          — lista verificações com filtros e paginação
/// - GET  /api/v1/contracts/verifications/{id}     — obtém detalhe completo de uma verificação
///
/// Política de autorização:
/// - Execução de verificação exige "contracts:verify".
/// - Consultas exigem "contracts:read".
/// </summary>
public sealed class ContractVerificationEndpointModule
{
    /// <summary>Registra os endpoints de verificação de compliance no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts/verifications");

        // POST /api/v1/contracts/verifications
        group.MapPost("/", async (
            VerifyComplianceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:verify");

        // GET /api/v1/contracts/verifications?serviceName=&apiAssetId=&page=&pageSize=
        group.MapGet("/", async (
            string? serviceName,
            string? apiAssetId,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListVerificationsFeature.Query(serviceName, apiAssetId, page ?? 1, pageSize ?? 20),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // GET /api/v1/contracts/verifications/{verificationId}
        group.MapGet("/{verificationId:guid}", async (
            Guid verificationId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetVerificationDetailFeature.Query(verificationId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
