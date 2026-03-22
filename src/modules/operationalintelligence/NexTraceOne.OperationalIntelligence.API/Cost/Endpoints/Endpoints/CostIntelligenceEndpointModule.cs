using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using IngestCostSnapshotFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.IngestCostSnapshot.IngestCostSnapshot;
using GetCostReportFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostReport.GetCostReport;
using GetCostByReleaseFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRelease.GetCostByRelease;
using GetCostByRouteFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostByRoute.GetCostByRoute;
using GetCostDeltaFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCostDelta.GetCostDelta;
using AttributeCostToServiceFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.AttributeCostToService.AttributeCostToService;
using ComputeCostTrendFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.ComputeCostTrend.ComputeCostTrend;
using AlertCostAnomalyFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.AlertCostAnomaly.AlertCostAnomaly;
using ImportCostBatchFeature = NexTraceOne.OperationalIntelligence.Application.Cost.Features.ImportCostBatch.ImportCostBatch;

namespace NexTraceOne.OperationalIntelligence.API.Cost.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo CostIntelligence.
/// Agrupa endpoints por responsabilidade funcional: ingestão, consulta, análise e alertas.
///
/// Endpoints disponíveis:
/// - POST   /snapshots          → Ingerir snapshot de custo
/// - POST   /import             → Importar batch de registos de custo reais
/// - GET    /report              → Relatório de custo por serviço/ambiente
/// - GET    /by-release/{id}     → Custo por release
/// - GET    /by-route            → Custo por rota/serviço
/// - GET    /delta               → Delta de custo entre períodos
/// - POST   /attributions        → Atribuir custo a serviço
/// - POST   /trends              → Computar tendência de custo
/// - POST   /anomaly-check       → Verificar anomalia de custo/orçamento
/// </summary>
public sealed class CostIntelligenceEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cost");

        group.MapPost("/snapshots", async (
            IngestCostSnapshotFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult("/api/v1/cost/snapshots/{0}", localizer);
        })
        .RequirePermission("operations:cost:write");

        group.MapGet("/report", async (
            string serviceName,
            string environment,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetCostReportFeature.Query(serviceName, environment, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:cost:read");

        group.MapGet("/by-release/{releaseId:guid}", async (
            Guid releaseId,
            DateTimeOffset periodStart,
            DateTimeOffset periodEnd,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetCostByReleaseFeature.Query(releaseId, periodStart, periodEnd);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:cost:read");

        group.MapGet("/by-route", async (
            string serviceName,
            string environment,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetCostByRouteFeature.Query(serviceName, environment, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:cost:read");

        group.MapGet("/delta", async (
            string serviceName,
            string environment,
            DateTimeOffset currentStart,
            DateTimeOffset currentEnd,
            DateTimeOffset previousStart,
            DateTimeOffset previousEnd,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetCostDeltaFeature.Query(serviceName, environment, currentStart, currentEnd, previousStart, previousEnd);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:cost:read");

        group.MapPost("/attributions", async (
            AttributeCostToServiceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult("/api/v1/cost/attributions/{0}", localizer);
        })
        .RequirePermission("operations:cost:write");

        group.MapPost("/trends", async (
            ComputeCostTrendFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult("/api/v1/cost/trends/{0}", localizer);
        })
        .RequirePermission("operations:cost:write");

        group.MapPost("/import", async (
            ImportCostBatchFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult("/api/v1/cost/import/{0}", localizer);
        })
        .RequirePermission("operations:cost:write");

        group.MapPost("/anomaly-check", async (
            AlertCostAnomalyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:cost:write");
    }
}
