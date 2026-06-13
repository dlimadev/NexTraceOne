using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GenerateCodeFromContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateCodeFromContract.GenerateCodeFromContract;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Endpoint de geração de código a partir de um contrato OpenAPI (contract-first determinístico).
///
/// - POST /api/v1/contracts/generate-code — gera DTOs + endpoints .NET (Clean Architecture)
///   a partir do contrato OpenAPI, sem IA.
///
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Wave AQ.4 — Contract-first deterministic code generation.
/// </summary>
public sealed class ContractCodeGenEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts")
            .WithTags("Contract Code Generation");

        group.MapPost("/generate-code", async (
            GenerateCodeFromContractFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
