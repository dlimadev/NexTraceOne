using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using EvaluateTemplateQualityGatesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateTemplateQualityGates.EvaluateTemplateQualityGates;
using GetCodeQualityReportFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCodeQualityReport.GetCodeQualityReport;
using IngestCodeQualityRecordFeature = NexTraceOne.Catalog.Application.Contracts.Features.IngestCodeQualityRecord.IngestCodeQualityRecord;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Endpoints de qualidade de código e análise estática.
///
/// - POST /api/v1/quality/records  — ingere um registo de qualidade de código
/// - GET  /api/v1/quality/report   — relatório de portfólio de qualidade por tenant
///
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public sealed class CodeQualityEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/quality")
            .WithTags("Code Quality");

        // ── Ingestão ─────────────────────────────────────────────────────
        group.MapPost("/records", async (
            IngestCodeQualityRecordFeature.Command cmd,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return result.ToCreatedResult($"/api/v1/quality/records/{result.Value}", localizer);
        }).RequirePermission("contracts:write");

        // ── Relatório de portfólio ────────────────────────────────────────
        group.MapGet("/report", async (
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCodeQualityReportFeature.Query(tenantId), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Quality gate do template (laço de governança) ─────────────────
        group.MapGet("/services/{serviceId}/gate", async (
            string serviceId,
            string tenantId,
            Guid? templateId,
            string? templateSlug,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new EvaluateTemplateQualityGatesFeature.Query(serviceId, tenantId, templateId, templateSlug), ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
