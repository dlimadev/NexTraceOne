using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using AttachCiCdEvidenceFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.AttachCiCdEvidence.AttachCiCdEvidence;
using GenerateEvidencePackFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.GenerateEvidencePack.GenerateEvidencePack;
using GetEvidencePackFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetEvidencePack.GetEvidencePack;
using ExportEvidencePackPdfFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ExportEvidencePackPdf.ExportEvidencePackPdf;

namespace NexTraceOne.ChangeGovernance.API.Workflow.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de Evidence Packs no ciclo de vida do workflow.
/// Evidence Packs reúnem os artefactos de auditoria de um workflow concluído:
/// diff semântico, blast radius, aprovações, observações e score de risco.
///
/// Funcionalidade crítica para auditores e tech leads que necessitam de
/// rastreabilidade completa das decisões de release.
/// </summary>
internal static class EvidencePackEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de evidence pack no grupo raiz do workflow.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/{instanceId:guid}/evidence-pack", async (
            Guid instanceId,
            GenerateEvidencePackFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { WorkflowInstanceId = instanceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult("/api/v1/workflow/{0}/evidence-pack", localizer);
        }).RequirePermission("workflow:instances:write");

        group.MapGet("/{instanceId:guid}/evidence-pack", async (
            Guid instanceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetEvidencePackFeature.Query(instanceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:read");

        group.MapGet("/{instanceId:guid}/evidence-pack/export", async (
            Guid instanceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportEvidencePackPdfFeature.Query(instanceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:read");

        // ── Endpoint CI/CD (P5.4) ─────────────────────────────────────────────
        group.MapPost("/{instanceId:guid}/evidence-pack/cicd", async (
            Guid instanceId,
            AttachCiCdEvidenceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { WorkflowInstanceId = instanceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:write");
    }
}
