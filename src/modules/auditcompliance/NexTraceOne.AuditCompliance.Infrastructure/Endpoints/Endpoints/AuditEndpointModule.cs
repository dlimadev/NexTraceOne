using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using ExportAuditReportFeature = NexTraceOne.Audit.Application.Features.ExportAuditReport.ExportAuditReport;
using GetAuditTrailFeature = NexTraceOne.Audit.Application.Features.GetAuditTrail.GetAuditTrail;
using GetComplianceReportFeature = NexTraceOne.Audit.Application.Features.GetComplianceReport.GetComplianceReport;
using RecordAuditEventFeature = NexTraceOne.Audit.Application.Features.RecordAuditEvent.RecordAuditEvent;
using SearchAuditLogFeature = NexTraceOne.Audit.Application.Features.SearchAuditLog.SearchAuditLog;
using VerifyChainIntegrityFeature = NexTraceOne.Audit.Application.Features.VerifyChainIntegrity.VerifyChainIntegrity;

namespace NexTraceOne.Audit.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Audit.
/// </summary>
public sealed class AuditEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit");

        group.MapPost("/events", async (
            RecordAuditEventFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/trail", async (
            string resourceType,
            string resourceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetAuditTrailFeature.Query(resourceType, resourceId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/search", async (
            string? sourceModule,
            string? actionType,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SearchAuditLogFeature.Query(sourceModule, actionType, from, to, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/verify-chain", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifyChainIntegrityFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/report", async (
            DateTimeOffset from,
            DateTimeOffset to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportAuditReportFeature.Query(from, to), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/compliance", async (
            DateTimeOffset from,
            DateTimeOffset to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetComplianceReportFeature.Query(from, to), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
