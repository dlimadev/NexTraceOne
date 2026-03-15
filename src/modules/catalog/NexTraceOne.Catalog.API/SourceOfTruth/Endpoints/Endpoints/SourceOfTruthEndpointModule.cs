using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetServiceSourceOfTruthFeature = NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceSourceOfTruth.GetServiceSourceOfTruth;
using GetContractSourceOfTruthFeature = NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetContractSourceOfTruth.GetContractSourceOfTruth;
using GetServiceCoverageFeature = NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceCoverage.GetServiceCoverage;
using SearchSourceOfTruthFeature = NexTraceOne.Catalog.Application.SourceOfTruth.Features.SearchSourceOfTruth.SearchSourceOfTruth;

namespace NexTraceOne.Catalog.API.SourceOfTruth.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API do módulo Source of Truth.
/// Fornece visões consolidadas de serviços e contratos, indicadores de cobertura
/// e pesquisa unificada para o NexTraceOne como fonte de verdade.
///
/// Política de autorização:
/// - Todos os endpoints exigem "catalog:assets:read" pois consultam dados de catálogo.
/// </summary>
public sealed class SourceOfTruthEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/source-of-truth");

        // ── Visão consolidada de serviço ────────────────────────────

        group.MapGet("/services/{serviceId:guid}", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetServiceSourceOfTruthFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Visão consolidada de contrato ───────────────────────────

        group.MapGet("/contracts/{contractVersionId:guid}", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetContractSourceOfTruthFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Indicadores de cobertura do serviço ─────────────────────

        group.MapGet("/services/{serviceId:guid}/coverage", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetServiceCoverageFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");

        // ── Pesquisa unificada de descoberta ────────────────────────

        group.MapGet("/search", async (
            string q,
            string? scope,
            int? maxResults,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SearchSourceOfTruthFeature.Query(q, scope, maxResults ?? 20), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:assets:read");
    }
}
