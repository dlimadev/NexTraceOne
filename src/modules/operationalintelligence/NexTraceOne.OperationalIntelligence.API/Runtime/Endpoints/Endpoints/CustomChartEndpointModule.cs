using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using CreateCustomChartFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateCustomChart.CreateCustomChart;
using ListCustomChartsFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListCustomCharts.ListCustomCharts;
using DeleteCustomChartFeature = NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DeleteCustomChart.DeleteCustomChart;

namespace NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints.Endpoints;

/// <summary>
/// Endpoints de Custom Charts do módulo OperationalIntelligence.
/// Permite criação, listagem e remoção de gráficos customizados por utilizador.
/// </summary>
public sealed class CustomChartEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/custom-charts");

        group.MapPost("/", async (
            CreateCustomChartFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(r => $"/api/v1/custom-charts/{r.ChartId}", localizer);
        }).RequirePermission("operations:runtime:write");

        group.MapGet("/", async (
            string userId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new ListCustomChartsFeature.Query(userId, tenantId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:runtime:read");

        group.MapDelete("/{chartId:guid}", async (
            Guid chartId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var command = new DeleteCustomChartFeature.Command(chartId, tenantId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:runtime:write");
    }
}
