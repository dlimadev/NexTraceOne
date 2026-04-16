using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Governance.Application.Features.GetIngestedServices;
using NexTraceOne.Governance.Application.Features.IngestOtelMetrics;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Telemetria — ingestão de métricas OTLP enviadas pelo OpenTelemetry Collector.
/// Funciona como receiver simplificado: o Collector exporta para este endpoint via OTLP/HTTP.
/// Destinado a Platform Admins e configuração de pipeline de observabilidade.
/// </summary>
public sealed class OtelTelemetryEndpointModule
{
    /// <summary>Registra endpoints de ingestão de telemetria no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var telemetry = app.MapGroup("/api/v1/telemetry");

        // Ingestão de métricas OTLP (batch)
        telemetry.MapPost("/metrics", async (
            IngestOtelMetrics.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:telemetry:ingest");

        // Consulta de nomes de serviços com métricas ingeridas
        telemetry.MapGet("/metrics/services", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetIngestedServices.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("platform:admin:read");
    }
}
