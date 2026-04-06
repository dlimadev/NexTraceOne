using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

using AcknowledgeFeature = NexTraceOne.Governance.Application.SecurityGate.Features.AcknowledgeFinding.AcknowledgeFinding;
using DashboardFeature = NexTraceOne.Governance.Application.SecurityGate.Features.GetSecurityDashboard.GetSecurityDashboard;
using EvaluateFeature = NexTraceOne.Governance.Application.SecurityGate.Features.EvaluateSecurityGate.EvaluateSecurityGate;
using GetReportFeature = NexTraceOne.Governance.Application.SecurityGate.Features.GenerateSecurityReport.GenerateSecurityReport;
using GetScanFeature = NexTraceOne.Governance.Application.SecurityGate.Features.GetSecurityScanResult.GetSecurityScanResult;
using ListFindingsFeature = NexTraceOne.Governance.Application.SecurityGate.Features.ListSecurityFindings.ListSecurityFindings;
using ScanCodeFeature = NexTraceOne.Governance.Application.SecurityGate.Features.ScanGeneratedCode.ScanGeneratedCode;
using ScanContractFeature = NexTraceOne.Governance.Application.SecurityGate.Features.ScanContractSecurity.ScanContractSecurity;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints do Security Gate Pipeline — SAST, scan de contratos, achados e dashboard de segurança.
/// </summary>
public sealed class SecurityGateEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/security");

        // Scan de código gerado
        group.MapPost("/scan/code", async (
            ScanCodeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:scan");

        // Scan de segurança de contrato
        group.MapPost("/scan/contract", async (
            ScanContractFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:scan");

        // Obter resultado de scan
        group.MapGet("/scans/{scanId:guid}", async (
            Guid scanId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetScanFeature.Query(scanId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:read");

        // Listar achados com filtros
        group.MapGet("/findings", async (
            Guid? targetId,
            FindingSeverity minSeverity,
            SecurityCategory? category,
            FindingStatus? status,
            int pageSize,
            int pageNumber,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListFindingsFeature.Query(targetId, minSeverity, category, status, pageSize, pageNumber);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:read");

        // Reconhecer achado
        group.MapPost("/findings/{scanId:guid}/{findingId:guid}/acknowledge", async (
            Guid scanId,
            Guid findingId,
            AcknowledgeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { ScanId = scanId, FindingId = findingId }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:write");

        // Dashboard global
        group.MapGet("/dashboard", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DashboardFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:read");

        // Re-avaliar gate com thresholds custom
        group.MapPost("/gate/evaluate", async (
            EvaluateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:write");

        // Gerar relatório de segurança
        group.MapGet("/report/{scanId:guid}", async (
            Guid scanId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetReportFeature.Query(scanId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:security:read");
    }
}
