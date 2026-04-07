using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetParameterUsageReportFeature = NexTraceOne.Configuration.Application.Features.GetParameterUsageReport.GetParameterUsageReport;
using GetParameterComplianceSummaryFeature = NexTraceOne.Configuration.Application.Features.GetParameterComplianceSummary.GetParameterComplianceSummary;
using TrackPersonaActivityFeature = NexTraceOne.Configuration.Application.Features.TrackPersonaActivity.TrackPersonaActivity;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// Registra endpoints Minimal API para analytics e compliance de parametrização.
/// GET /api/v1/configuration/analytics/usage — relatório de utilização de parâmetros.
/// GET /api/v1/configuration/analytics/compliance — resumo de compliance.
/// GET /api/v1/configuration/analytics/persona-activity — atividade por persona.
/// </summary>
public sealed class ConfigurationAnalyticsEndpointModule
{
    /// <summary>Registra os endpoints de analytics no roteador.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/configuration/analytics");

        // GET /api/v1/configuration/analytics/usage
        group.MapGet("/usage", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetParameterUsageReportFeature.Query(),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:analytics:read");

        // GET /api/v1/configuration/analytics/compliance
        group.MapGet("/compliance", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetParameterComplianceSummaryFeature.Query(),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:analytics:read");

        // GET /api/v1/configuration/analytics/persona-activity?key={key}&limit={limit}
        group.MapGet("/persona-activity", async (
            string? key,
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new TrackPersonaActivityFeature.Query(key, limit ?? 100),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("configuration:analytics:read");
    }
}
